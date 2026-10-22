using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SistemaDeVentas.DTOs.Financiaciones;
using SistemaDeVentas.DTOs.Ventas;
using SistemaDeVentas.Entities;
using Xunit.Abstractions;

namespace OptiGest.Tests.Sql;

[Collection("TestingSql"), Trait("Suite", "Sql")]
public class IntegrityTests(TestingDatabase database, ITestOutputHelper output)
{
    [SqlFact]
    public async Task Owned_database_uses_sql_server_and_existing_migrations()
    {
        await database.VerifyOwnershipAsync();
        await using var db = await database.CreateContextAsync();
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", db.Database.ProviderName);
        Assert.Equal(2, (await db.Database.GetAppliedMigrationsAsync()).Count());
        Assert.False(db.Database.CreateExecutionStrategy().RetriesOnFailure);
        output.WriteLine($"Testing database: {database.DatabaseName}");
    }

    [SqlFact]
    public async Task Successful_financing_has_complete_plan_with_exact_sum()
    {
        await using var writer = await database.CreateContextAsync();
        var seed = await SyntheticCase.SeedAsync(writer);
        writer.ChangeTracker.Clear();
        var result = await TestingDatabase.Financing(writer).CrearAsync(new CrearFinanciacionDto
        { VentaId = seed.SaleId, ConfiguracionFinanciacionId = seed.PlanId });
        await using var reader = await database.CreateContextAsync();
        var stored = await reader.Financiaciones.Include(f => f.Cuotas).SingleAsync(f => f.Id == result.Id);
        Assert.Equal(3, stored.Cuotas.Count);
        Assert.Equal(stored.MontoTotal, stored.Cuotas.Sum(c => c.Monto));
        Assert.Equal(1100m, await reader.CuentasCorrientes.Where(c => c.ClienteId == seed.ClientId).SumAsync(c => c.Debe));
    }

    private async Task<(int Financing, int Installments, decimal Interest)> FailFinancing()
    {
        await using var seedDb = await database.CreateContextAsync();
        var seed = await SyntheticCase.SeedAsync(seedDb);
        var fault = new FailOnWrite(FailOnWrite.SecondInstallment);
        await using (var writer = await database.CreateContextAsync(fault))
        {
            await Assert.ThrowsAsync<InjectedFailure>(() => TestingDatabase.Financing(writer).CrearAsync(new CrearFinanciacionDto
            { VentaId = seed.SaleId, ConfiguracionFinanciacionId = seed.PlanId }));
        }
        Assert.True(fault.Triggered);
        await using var fresh = await database.CreateContextAsync();
        return (await fresh.Financiaciones.CountAsync(f => f.VentaId == seed.SaleId),
            await fresh.Cuotas.CountAsync(c => c.Financiacion.VentaId == seed.SaleId),
            await fresh.CuentasCorrientes.Where(c => c.ClienteId == seed.ClientId).SumAsync(c => c.Debe) - 1000m);
    }

    [SqlFact]
    public async Task Required_financing_failure_rolls_back_every_write()
    {
        var state = await FailFinancing();
        Assert.Equal((0, 0, 0m), state);
    }

    private async Task<(int Sales, int Payments, int Stock, decimal Debt)> FailSale()
    {
        await using var seedDb = await database.CreateContextAsync();
        var seed = await SyntheticCase.SeedAsync(seedDb);
        var fault = new FailOnWrite(FailOnWrite.LedgerCredit);
        await using (var writer = await database.CreateContextAsync(fault))
        {
            await Assert.ThrowsAsync<InjectedFailure>(() => TestingDatabase.Sales(writer).CrearAsync(new CrearVentaDto
            {
                ClienteId = seed.ClientId,
                Detalles = [new() { ProductoId = seed.ProductId, Cantidad = 2 }],
                PagoInicial = new PagoInicialVentaDto { Monto = 50m, FormaPago = "Efectivo" }
            }, seed.UserId));
        }
        Assert.True(fault.Triggered);
        await using var fresh = await database.CreateContextAsync();
        return (await fresh.Ventas.CountAsync(v => v.ClienteId == seed.ClientId && v.Id != seed.SaleId),
            await fresh.PagosVenta.CountAsync(p => p.Venta.ClienteId == seed.ClientId),
            (await fresh.Productos.SingleAsync(p => p.Id == seed.ProductId)).Stock,
            await fresh.CuentasCorrientes.Where(c => c.ClienteId == seed.ClientId).SumAsync(c => c.Debe - c.Haber) - 1000m);
    }

    [SqlFact]
    public async Task Required_sale_failure_rolls_back_every_write()
    {
        Assert.Equal((0, 0, 10, 0m), await FailSale());
    }

    [SqlFact]
    public async Task Sql_transaction_rollback_is_observable_from_fresh_context()
    {
        await using var seedDb = await database.CreateContextAsync();
        var seed = await SyntheticCase.SeedAsync(seedDb);
        await using (var writer = await database.CreateContextAsync())
        {
            await using var transaction = await writer.Database.BeginTransactionAsync();
            var product = await writer.Productos.SingleAsync(p => p.Id == seed.ProductId);
            product.Stock = 1;
            await writer.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        await using var fresh = await database.CreateContextAsync();
        Assert.Equal(10, (await fresh.Productos.SingleAsync(p => p.Id == seed.ProductId)).Stock);
    }

    [SqlFact]
    public async Task Concurrent_scopes_use_distinct_contexts_and_connections()
    {
        await using var first = await database.CreateContextAsync();
        await using var second = await database.CreateContextAsync();
        Assert.NotEqual(first.ContextId, second.ContextId);
        Assert.NotSame(first.Database.GetDbConnection(), second.Database.GetDbConnection());
        var counts = await Task.WhenAll(first.Productos.CountAsync(), second.Productos.CountAsync());
        Assert.Equal(counts[0], counts[1]);
    }
}
