using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SistemaDeVentas.Data;
using SistemaDeVentas.DTOs.Financiaciones;
using SistemaDeVentas.DTOs.PagosVenta;
using SistemaDeVentas.DTOs.PagoCuota;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Helpers;
using SistemaDeVentas.Repositories;

namespace OptiGest.Tests.Sql;

[Collection("TestingSql"), Trait("Suite", "Sql")]
public class FinancialPhaseOneTests(TestingDatabase database)
{
    private async Task<SyntheticCase> Seed()
    {
        await using var db = await database.CreateContextAsync();
        return await SyntheticCase.SeedAsync(db);
    }

    private async Task<int> Finance(SyntheticCase seed, params IInterceptor[] interceptors)
    {
        await using var db = await database.CreateContextAsync(interceptors);
        return (await TestingDatabase.Financing(db).CrearAsync(new CrearFinanciacionDto
        { VentaId = seed.SaleId, ConfiguracionFinanciacionId = seed.PlanId })).Id;
    }

    private async Task Pay(SyntheticCase seed, decimal amount, params IInterceptor[] interceptors)
    {
        await using var db = await database.CreateContextAsync(interceptors);
        await TestingDatabase.Payments(db).CrearAsync(new PagoVentaCreateDto
        { VentaId = seed.SaleId, Monto = amount, FormaPago = "Efectivo" });
    }

    private async Task PayInstallment(int id, decimal amount, params IInterceptor[] interceptors)
    {
        await using var db = await database.CreateContextAsync(interceptors);
        await TestingDatabase.InstallmentPayments(db).CrearAsync(new CrearPagoCuotaDto
        { CuotaId = id, Monto = amount, FormaPago = "Efectivo" });
    }

    private static async Task<string> Attempt(Func<Task> action)
    {
        try { await action(); return "ok"; }
        catch (ReglaFinancieraException error) when (error.EstadoHttp == 409) { return error.Codigo; }
        // SQL errors, assertion failures, cancellation and unknown outcomes are NOT accepted as conflicts.
    }

    [SqlFact]
    public async Task Interest_write_failure_rolls_back_and_explicit_retry_creates_once()
    {
        var seed = await Seed();
        var fault = new FailOnWrite(db => db.ChangeTracker.Entries<CuentaCorriente>().Any(e => e.State == EntityState.Added && e.Entity.Debe > 0));
        await Assert.ThrowsAsync<InjectedFailure>(() => Finance(seed, fault));
        Assert.True(fault.Triggered);
        await using (var reader = await database.CreateContextAsync())
        {
            Assert.False(await reader.Financiaciones.AnyAsync(f => f.VentaId == seed.SaleId));
            Assert.Equal(1, await reader.CuentasCorrientes.CountAsync(c => c.ClienteId == seed.ClientId));
        }
        await Finance(seed);
        Assert.Equal("financiacion_existente", await Attempt(async () => { await Finance(seed); }));
        await using var fresh = await database.CreateContextAsync();
        Assert.Equal(1, await fresh.Financiaciones.CountAsync(f => f.VentaId == seed.SaleId));
        Assert.Equal(3, await fresh.Cuotas.CountAsync(c => c.Financiacion.VentaId == seed.SaleId));
        Assert.Equal(2, await fresh.CuentasCorrientes.CountAsync(c => c.ClienteId == seed.ClientId));
    }

    [SqlFact]
    public async Task Two_financings_create_one_complete_plan_and_one_interest_movement()
    {
        var seed = await Seed();
        var results = await Task.WhenAll(Attempt(async () => { await Finance(seed); }), Attempt(async () => { await Finance(seed); }));
        Assert.Single(results, result => result == "ok");
        Assert.Single(results, result => result == "financiacion_existente");
        await using var db = await database.CreateContextAsync();
        var financing = await db.Financiaciones.Include(f => f.Cuotas).SingleAsync(f => f.VentaId == seed.SaleId);
        Assert.Equal(3, financing.Cuotas.Count);
        Assert.Equal(financing.MontoTotal, financing.Cuotas.Sum(c => c.Monto));
        Assert.Equal(1100m, await db.CuentasCorrientes.Where(c => c.ClienteId == seed.ClientId).SumAsync(c => c.Debe - c.Haber));
    }

    [SqlFact]
    public async Task Concurrent_payment_and_financing_use_the_serialized_remaining_balance()
    {
        var seed = await Seed();
        var results = await Task.WhenAll(Attempt(() => Pay(seed, 800m)), Attempt(async () => { await Finance(seed); }));
        Assert.Equal("ok", results[1]);
        Assert.Contains(results[0], new[] { "ok", "venta_financiada" });
        await using var db = await database.CreateContextAsync();
        var paid = await db.PagosVenta.Where(p => p.VentaId == seed.SaleId).SumAsync(p => p.Monto);
        var financing = await db.Financiaciones.SingleAsync(f => f.VentaId == seed.SaleId);
        Assert.Equal(1000m - paid, financing.MontoFinanciado);
        Assert.Equal(1000m - paid + financing.MontoTotal - financing.MontoFinanciado,
            await db.CuentasCorrientes.Where(c => c.ClienteId == seed.ClientId).SumAsync(c => c.Debe - c.Haber));
    }

    [SqlTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Direct_payment_is_forbidden_after_financing_even_if_cancelled(bool cancelled)
    {
        var seed = await Seed();
        await Pay(seed, 200m);
        var id = await Finance(seed);
        if (cancelled)
        {
            await using var setup = await database.CreateContextAsync();
            var financing = await setup.Financiaciones.FindAsync(id);
            financing!.Estado = "Cancelada";
            await setup.SaveChangesAsync(); // Synthetic historical state, no production data.
        }
        Assert.Equal("venta_financiada", await Attempt(() => Pay(seed, 100m)));
        await using var db = await database.CreateContextAsync();
        Assert.Equal(200m, await db.PagosVenta.Where(p => p.VentaId == seed.SaleId).SumAsync(p => p.Monto));
        Assert.Equal(3, await db.CuentasCorrientes.CountAsync(c => c.ClienteId == seed.ClientId));
    }

    [SqlFact]
    public async Task Direct_payment_failure_rolls_back_payment_state_and_ledger()
    {
        var seed = await Seed();
        var fault = new FailOnWrite(FailOnWrite.LedgerCredit);
        await Assert.ThrowsAsync<InjectedFailure>(() => Pay(seed, 1000m, fault));
        Assert.True(fault.Triggered);
        await using var db = await database.CreateContextAsync();
        Assert.False(await db.PagosVenta.AnyAsync(p => p.VentaId == seed.SaleId));
        Assert.Equal("Pendiente", (await db.Ventas.FindAsync(seed.SaleId))!.Estado);
        Assert.Equal(1000m, await db.CuentasCorrientes.Where(c => c.ClienteId == seed.ClientId).SumAsync(c => c.Debe - c.Haber));
    }

    [SqlTheory]
    [InlineData("payment")]
    [InlineData("installment")]
    [InlineData("financing")]
    [InlineData("sale")]
    [InlineData("ledger")]
    public async Task Installment_payment_failure_rolls_back_every_stage(string stage)
    {
        var seed = await Seed();
        await using (var setup = await database.CreateContextAsync())
        {
            (await setup.ConfiguracionesFinanciacion.FindAsync(seed.PlanId))!.CantidadCuotas = 1;
            await setup.SaveChangesAsync();
        }
        var id = await Finance(seed);
        await using var before = await database.CreateContextAsync();
        var installment = await before.Cuotas.SingleAsync(c => c.FinanciacionId == id);
        var fault = new FailOnWrite(db => stage switch
        {
            "payment" => db.ChangeTracker.Entries<PagoCuota>().Any(e => e.State == EntityState.Added),
            "installment" => db.ChangeTracker.Entries<Cuota>().Any(e => e.State == EntityState.Modified),
            "financing" => db.ChangeTracker.Entries<Financiacion>().Any(e => e.State == EntityState.Modified),
            "sale" => db.ChangeTracker.Entries<Venta>().Any(e => e.State == EntityState.Modified),
            _ => FailOnWrite.LedgerCredit(db)
        });
        await Assert.ThrowsAsync<InjectedFailure>(() => PayInstallment(installment.Id, installment.Monto, fault));
        Assert.True(fault.Triggered);
        await using var db = await database.CreateContextAsync();
        Assert.False(await db.PagosCuota.AnyAsync(p => p.CuotaId == installment.Id));
        Assert.Equal("Pendiente", (await db.Cuotas.FindAsync(installment.Id))!.Estado);
        Assert.Equal("Activa", (await db.Financiaciones.FindAsync(id))!.Estado);
        Assert.Equal("Pendiente", (await db.Ventas.FindAsync(seed.SaleId))!.Estado);
        Assert.Equal(1100m, await db.CuentasCorrientes.Where(c => c.ClienteId == seed.ClientId).SumAsync(c => c.Debe - c.Haber));
    }

    [SqlFact]
    public async Task Concurrent_double_installment_payment_creates_only_one_credit()
    {
        var seed = await Seed();
        var id = await Finance(seed);
        await using var before = await database.CreateContextAsync();
        var installment = await before.Cuotas.OrderBy(c => c.Numero).FirstAsync(c => c.FinanciacionId == id);
        var results = await Task.WhenAll(Attempt(() => PayInstallment(installment.Id, installment.Monto)),
            Attempt(() => PayInstallment(installment.Id, installment.Monto)));
        Assert.Single(results, result => result == "ok");
        Assert.Single(results, result => result == "cuota_pagada");
        await using var db = await database.CreateContextAsync();
        Assert.Equal(1, await db.PagosCuota.CountAsync(p => p.CuotaId == installment.Id));
        Assert.Equal(installment.Monto, await db.CuentasCorrientes.Where(c => c.ClienteId == seed.ClientId).SumAsync(c => c.Haber));
    }

    [SqlFact]
    public async Task Last_installments_cancel_financing_and_sale_and_leave_zero_balance()
    {
        var seed = await Seed();
        var id = await Finance(seed);
        await using var before = await database.CreateContextAsync();
        var installments = await before.Cuotas.Where(c => c.FinanciacionId == id).OrderBy(c => c.Numero).ToListAsync();
        Assert.Equal("monto_cuota_incorrecto", await Attempt(() => PayInstallment(installments[0].Id, installments[0].Monto - 1m)));
        await PayInstallment(installments[0].Id, installments[0].Monto);
        await Task.WhenAll(PayInstallment(installments[1].Id, installments[1].Monto), PayInstallment(installments[2].Id, installments[2].Monto));
        await using var db = await database.CreateContextAsync();
        Assert.Equal(3, await db.PagosCuota.CountAsync(p => p.Cuota.FinanciacionId == id));
        Assert.Equal("Cancelada", (await db.Financiaciones.FindAsync(id))!.Estado);
        Assert.Equal("Pagada", (await db.Ventas.FindAsync(seed.SaleId))!.Estado);
        Assert.Equal(0m, await db.CuentasCorrientes.Where(c => c.ClienteId == seed.ClientId).SumAsync(c => c.Debe - c.Haber));
    }

    [SqlFact]
    public async Task Two_sales_of_one_client_keep_running_balances_consistent()
    {
        var seed = await Seed();
        await using var setup = await database.CreateContextAsync();
        var otherSale = new Venta { ClienteId = seed.ClientId, UsuarioId = seed.UserId, Estado = "Pendiente", Total = 1000m, Subtotal = 1000m, Fecha = DateTime.Now };
        otherSale.DetallesVenta.Add(new DetalleVenta { ProductoId = seed.ProductId, Cantidad = 1, PrecioUnitario = 1000m });
        setup.Ventas.Add(otherSale);
        await setup.SaveChangesAsync();
        await TestingDatabase.Ledger(setup).CrearAsync(new CuentaCorriente
        { ClienteId = seed.ClientId, Fecha = DateTime.Now.AddDays(-1), Concepto = "Synthetic second sale", Debe = 1000m });
        var other = seed with { SaleId = otherSale.Id };
        await Task.WhenAll(Pay(seed, 100m), Pay(other, 200m));
        await using var db = await database.CreateContextAsync();
        var movements = await db.CuentasCorrientes.Where(c => c.ClienteId == seed.ClientId).OrderBy(c => c.Id).ToListAsync();
        decimal balance = 0;
        foreach (var movement in movements)
        { balance += movement.Debe - movement.Haber; Assert.Equal(balance, movement.Saldo); }
        Assert.Equal(1700m, balance);
        var summary = await TestingDatabase.Ledger(db).ObtenerResumenAsync();
        Assert.Equal(balance, summary.Single(c => c.ClienteId == seed.ClientId).SaldoActual);
        var account = await new SistemaDeVentas.Services.CuentaCorrienteService(TestingDatabase.Ledger(db))
            .ObtenerCuentaClienteAsync(seed.ClientId);
        Assert.Equal(balance, account.SaldoActual);
        Assert.Equal(balance, await TestingDatabase.Ledger(db).ObtenerSaldoActualAsync(seed.ClientId));
        Assert.Equal(summary.Count(c => c.SaldoActual > 0), await TestingDatabase.Ledger(db).ContarClientesConDeudaAsync());
    }

    private sealed class CommitFailure : DbTransactionInterceptor
    {
        public int Calls { get; private set; }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        { Calls++; throw new IOException("Synthetic lost acknowledgement AFTER server commit"); }
    }

    [SqlFact]
    public async Task Unknown_commit_result_is_not_replayed_and_can_be_reconciled()
    {
        var seed = await Seed();
        var fault = new CommitFailure();
        var error = await Assert.ThrowsAsync<ResultadoFinancieroDesconocidoException>(() => Finance(seed, fault));
        Assert.Equal(503, error.EstadoHttp);
        Assert.Equal(1, fault.Calls);
        await using var db = await database.CreateContextAsync();
        var stored = await db.Financiaciones.Include(f => f.Cuotas).SingleAsync(f => f.VentaId == seed.SaleId);
        Assert.Equal(3, stored.Cuotas.Count);
        Assert.Equal("financiacion_existente", await Attempt(async () => { await Finance(seed); }));
    }

    [SqlFact]
    public async Task Lock_timeout_is_controlled_and_explicit_retry_after_release_succeeds()
    {
        var seed = await Seed();
        await using var blocker = await database.CreateContextAsync();
        await using var transaction = await blocker.Database.BeginTransactionAsync();
        await new OperacionFinancieraRepository(blocker).BloquearClienteAsync(seed.ClientId);
        Assert.Equal("cliente_ocupado", await Attempt(() => Pay(seed, 100m)));
        await transaction.RollbackAsync();
        await Pay(seed, 100m);
        await using var db = await database.CreateContextAsync();
        Assert.Equal(1, await db.PagosVenta.CountAsync(p => p.VentaId == seed.SaleId));
    }

    [SqlFact]
    public async Task A_different_client_is_not_blocked_by_an_unrelated_financial_operation()
    {
        var first = await Seed();
        var second = await Seed();
        await using var blocker = await database.CreateContextAsync();
        await using var transaction = await blocker.Database.BeginTransactionAsync();
        await new OperacionFinancieraRepository(blocker).BloquearClienteAsync(first.ClientId);
        await Pay(second, 100m).WaitAsync(TimeSpan.FromSeconds(5));
        await transaction.RollbackAsync();
    }

    [SqlFact]
    public async Task Automatic_retry_configuration_is_rejected_before_running_writes()
    {
        await using var owned = await database.CreateContextAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(owned.Database.GetConnectionString(), sql => sql.EnableRetryOnFailure()).Options;
        await using var retrying = new AppDbContext(options);
        var called = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => new OperacionFinancieraRepository(retrying)
            .EjecutarAsync(1, () => { called = true; return Task.FromResult(1); }));
        Assert.False(called);
    }

    [SqlFact]
    public async Task Sql_deadlock_is_a_controlled_conflict_not_an_unknown_commit_or_replay()
    {
        var a = await Seed();
        var b = await Seed();
        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrivals = 0;
        async Task<string> UpdatePair(SyntheticCase seed, int firstId, int secondId)
        {
            await using var db = await database.CreateContextAsync();
            return await Attempt(async () =>
            {
                await new OperacionFinancieraRepository(db).EjecutarAsync(seed.ClientId, async () =>
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Productos SET Stock=Stock WHERE Id={firstId}");
                    if (Interlocked.Increment(ref arrivals) == 2) barrier.TrySetResult();
                    await barrier.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Productos SET Stock=Stock WHERE Id={secondId}");
                    return true;
                });
            });
        }
        var results = await Task.WhenAll(UpdatePair(a, a.ProductId, b.ProductId), UpdatePair(b, b.ProductId, a.ProductId))
            .WaitAsync(TimeSpan.FromSeconds(40));
        Assert.Single(results, result => result == "ok");
        Assert.Single(results, result => result == "conflicto_concurrencia");
    }
}
