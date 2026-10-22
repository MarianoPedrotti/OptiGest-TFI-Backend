using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.DTOs.CuentasCorrientes;
using SistemaDeVentas.DTOs.PagosVenta;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories;
using SistemaDeVentas.Services;

namespace OptiGest.Tests.Sql;

[Collection("TestingSql"), Trait("Suite", "Sql")]
public class ReleaseFinancialRegressionTests(TestingDatabase database)
{
    [SqlTheory]
    [InlineData("Pendiente", 0, 100, "PagoParcial")]
    [InlineData("PagoParcial", 200, 100, "PagoParcial")]
    [InlineData("Parcial", 200, 100, "PagoParcial")]
    [InlineData("Pendiente", 0, 1000, "Pagada")]
    [InlineData("PagoParcial", 200, 800, "Pagada")]
    [InlineData("Parcial", 200, 800, "Pagada")]
    public async Task Subsequent_payments_write_canonical_state_and_preserve_historical_reads(
        string historicalState, int alreadyPaid, int payment, string expectedState)
    {
        await using var setup = await database.CreateContextAsync();
        var seed = await SyntheticCase.SeedAsync(setup);
        if (alreadyPaid > 0)
            await TestingDatabase.Payments(setup).CrearAsync(new PagoVentaCreateDto
            { VentaId = seed.SaleId, Monto = alreadyPaid, FormaPago = "Testing" });
        (await setup.Ventas.FindAsync(seed.SaleId))!.Estado = historicalState;
        await setup.SaveChangesAsync();

        await using (var reader = await database.CreateContextAsync())
        {
            Assert.Equal(historicalState, (await TestingDatabase.Sales(reader).ObtenerPorIdAsync(seed.SaleId))!.Estado);
            Assert.Equal(historicalState, (await TestingDatabase.Sales(reader).ObtenerTodosAsync()).Single(v => v.Id == seed.SaleId).Estado);
        }
        await using (var writer = await database.CreateContextAsync())
        {
            Assert.Equal(historicalState, (await writer.Ventas.FindAsync(seed.SaleId))!.Estado);
            await TestingDatabase.Payments(writer).CrearAsync(new PagoVentaCreateDto
            { VentaId = seed.SaleId, Monto = payment, FormaPago = "Testing" });
        }
        await using var fresh = await database.CreateContextAsync();
        Assert.Equal(expectedState, (await fresh.Ventas.FindAsync(seed.SaleId))!.Estado);
        Assert.Equal(alreadyPaid + payment, await fresh.PagosVenta.Where(p => p.VentaId == seed.SaleId).SumAsync(p => p.Monto));
        Assert.Equal(1000m - alreadyPaid - payment, await TestingDatabase.Ledger(fresh).ObtenerSaldoActualAsync(seed.ClientId));
    }

    [SqlFact]
    public async Task Sales_summary_counts_both_partial_aliases_without_rewriting_history()
    {
        await using var setup = await database.CreateContextAsync();
        var before = await new VentaRepository(setup).ObtenerResumenAsync();
        var states = new[] { "Pendiente", "PagoParcial", "Parcial", "Pagada" };
        var ids = new List<int>();
        foreach (var state in states)
        {
            var seed = await SyntheticCase.SeedAsync(setup);
            var paid = state == "Pendiente" ? 0 : state == "Pagada" ? 1000 : 200;
            if (paid > 0)
                await TestingDatabase.Payments(setup).CrearAsync(new PagoVentaCreateDto
                { VentaId = seed.SaleId, Monto = paid, FormaPago = "Testing" });
            (await setup.Ventas.FindAsync(seed.SaleId))!.Estado = state;
            await setup.SaveChangesAsync();
            ids.Add(seed.SaleId);
        }
        await using (var reader = await database.CreateContextAsync())
        {
            var result = await TestingDatabase.Sales(reader).ObtenerResumenAsync();
            Assert.Equal(before.Pendientes + 3, result.Pendientes);
            Assert.Equal(before.Pagadas + 1, result.Pagadas);
            Assert.Equal(before.VentasDelMes + 4, result.VentasDelMes);
            Assert.Equal(before.MontoVendidoMes + 4000m, result.MontoVendidoMes);
        }
        await using var fresh = await database.CreateContextAsync();
        Assert.Equal(states, await fresh.Ventas.Where(v => ids.Contains(v.Id)).OrderBy(v => v.Id).Select(v => v.Estado).ToArrayAsync());
    }

    private async Task<(int ClientId, decimal Expected)> SeedHistoricalAccount(int credit)
    {
        await using var db = await database.CreateContextAsync();
        var seed = await SyntheticCase.SeedAsync(db);
        var first = await db.CuentasCorrientes.SingleAsync(c => c.ClienteId == seed.ClientId);
        first.Fecha = new DateTime(2026, 10, 20);
        first.Saldo = -999m; // Deliberately inconsistent synthetic snapshot, never repaired by reads.
        await db.SaveChangesAsync();
        await TestingDatabase.Ledger(db).CrearAsync(new CuentaCorriente
        { ClienteId = seed.ClientId, Fecha = new DateTime(2026, 10, 19), Concepto = "Testing backdated credit", Haber = credit });
        var last = await db.CuentasCorrientes.Where(c => c.ClienteId == seed.ClientId).OrderByDescending(c => c.Id).FirstAsync();
        Assert.Equal(1000m - credit, last.Saldo); // New writes do not propagate bad snapshots.
        last.Saldo = credit < 1000 ? -777m : 777m;
        await db.SaveChangesAsync();
        return (seed.ClientId, 1000m - credit);
    }

    [SqlTheory]
    [InlineData(200)]
    [InlineData(1000)]
    [InlineData(1200)]
    public async Task Individual_account_uses_amounts_preserving_historical_snapshots(int credit)
    {
        var (clientId, expected) = await SeedHistoricalAccount(credit);
        await using (var reader = await database.CreateContextAsync())
        {
            var ledger = TestingDatabase.Ledger(reader);
            var account = await new CuentaCorrienteService(ledger).ObtenerCuentaClienteAsync(clientId);
            Assert.Equal(expected, account.SaldoActual);
            Assert.Equal(expected, await ledger.ObtenerSaldoActualAsync(clientId));
        }
        await using var fresh = await database.CreateContextAsync();
        var snapshots = await fresh.CuentasCorrientes.Where(c => c.ClienteId == clientId).OrderBy(c => c.Id).Select(c => c.Saldo).ToArrayAsync();
        Assert.Equal(new[] { -999m, credit < 1000 ? -777m : 777m }, snapshots);
    }

    [SqlTheory]
    [InlineData(200)]
    [InlineData(1000)]
    [InlineData(1200)]
    public async Task Account_summary_uses_amounts_instead_of_latest_snapshot(int credit)
    {
        var (clientId, expected) = await SeedHistoricalAccount(credit);
        await using var reader = await database.CreateContextAsync();
        var summary = await TestingDatabase.Ledger(reader).ObtenerResumenAsync();
        Assert.Equal(expected, summary.Single(c => c.ClienteId == clientId).SaldoActual);
        await using var fresh = await database.CreateContextAsync();
        Assert.Equal(new[] { -999m, credit < 1000 ? -777m : 777m },
            await fresh.CuentasCorrientes.Where(c => c.ClienteId == clientId).OrderBy(c => c.Id).Select(c => c.Saldo).ToArrayAsync());
    }

    [SqlTheory]
    [InlineData(200)]
    [InlineData(1000)]
    [InlineData(1200)]
    public async Task Debt_counter_uses_aggregate_balance(int credit)
    {
        await using var before = await database.CreateContextAsync();
        var initial = await TestingDatabase.Ledger(before).ContarClientesConDeudaAsync();
        await SeedHistoricalAccount(credit);
        await using var fresh = await database.CreateContextAsync();
        Assert.Equal(initial + (credit < 1000 ? 1 : 0), await TestingDatabase.Ledger(fresh).ContarClientesConDeudaAsync());
    }

    [SqlFact]
    public async Task Date_filters_do_not_change_current_balance_or_historical_rows()
    {
        await using var setup = await database.CreateContextAsync();
        var seed = await SyntheticCase.SeedAsync(setup);
        var first = await setup.CuentasCorrientes.SingleAsync(c => c.ClienteId == seed.ClientId);
        first.Fecha = new DateTime(2026, 10, 20);
        await setup.SaveChangesAsync();
        await TestingDatabase.Ledger(setup).CrearAsync(new CuentaCorriente
        { ClienteId = seed.ClientId, Fecha = new DateTime(2026, 10, 19), Concepto = "Testing backdated payment", Haber = 300m });
        await using var reader = await database.CreateContextAsync();
        var ledger = TestingDatabase.Ledger(reader);
        var service = new CuentaCorrienteService(ledger);
        var filtered = await service.ObtenerMovimientosAsync(new MovimientoCuentaCorrienteFiltroDto
        { ClienteId = seed.ClientId, Desde = new DateTime(2026, 10, 20), Hasta = new DateTime(2026, 10, 21) });
        Assert.Equal(1000m, Assert.Single(filtered).Saldo);
        Assert.Equal(700m, (await service.ObtenerCuentaClienteAsync(seed.ClientId)).SaldoActual);
        Assert.Equal(700m, await ledger.ObtenerSaldoActualAsync(seed.ClientId));
        Assert.Equal(700m, (await ledger.ObtenerResumenAsync()).Single(c => c.ClienteId == seed.ClientId).SaldoActual);
        Assert.Equal(700m, (await ledger.ObtenerUltimoMovimientoAsync(seed.ClientId))!.Saldo);
        await using var fresh = await database.CreateContextAsync();
        Assert.Equal(new[] { 1000m, 700m }, await fresh.CuentasCorrientes.Where(c => c.ClienteId == seed.ClientId).OrderBy(c => c.Id).Select(c => c.Saldo).ToArrayAsync());
    }

    [SqlFact]
    public async Task Equal_dates_have_id_tiebreakers_in_both_movement_lists()
    {
        await using var setup = await database.CreateContextAsync();
        var seed = await SyntheticCase.SeedAsync(setup);
        var first = await setup.CuentasCorrientes.SingleAsync(c => c.ClienteId == seed.ClientId);
        var date = first.Fecha;
        var second = await TestingDatabase.Ledger(setup).CrearAsync(new CuentaCorriente
        { ClienteId = seed.ClientId, Fecha = date, Concepto = "Testing equal date", Haber = 100m });
        var third = await TestingDatabase.Ledger(setup).CrearAsync(new CuentaCorriente
        { ClienteId = seed.ClientId, Fecha = date, Concepto = "Testing equal date", Haber = 200m });
        await using var reader = await database.CreateContextAsync();
        var ledger = TestingDatabase.Ledger(reader);
        Assert.Equal(new[] { first.Id, second.Id, third.Id }, (await ledger.ObtenerPorClienteAsync(seed.ClientId)).Select(c => c.Id));
        Assert.Equal(new[] { third.Id, second.Id, first.Id }, (await ledger.ObtenerMovimientosAsync(seed.ClientId, null, null)).Select(c => c.Id));
    }
}
