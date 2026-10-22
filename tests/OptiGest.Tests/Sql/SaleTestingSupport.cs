using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SistemaDeVentas.Data;
using SistemaDeVentas.DTOs.Ventas;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Helpers;

namespace OptiGest.Tests.Sql;

internal static class SaleTestingSupport
{
    public static async Task<SyntheticCase> Seed(TestingDatabase database)
    {
        await using var db = await database.CreateContextAsync();
        return await SyntheticCase.SeedAsync(db);
    }

    public static async Task<int> FreshVehicle(TestingDatabase database, SyntheticCase seed, string state = "Disponible")
    {
        await using var db = await database.CreateContextAsync();
        var modelId = (await db.Vehiculos.FindAsync(seed.VehicleId))!.ModeloId;
        var unique = Guid.NewGuid().ToString("N");
        var vehicle = new Vehiculo { ModeloId = modelId, VIN = unique, NumeroMotor = unique, Patente = unique,
            Estado = state, PrecioVenta = 1000m, PrecioCompra = 500m, Anio = 2026, Color = "Testing" };
        db.Vehiculos.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle.Id;
    }

    public static CrearVentaDto Articles(SyntheticCase seed, int productId, int quantity, decimal payment = 0) => new()
    {
        ClienteId = seed.ClientId,
        Detalles = [new() { ProductoId = productId, Cantidad = quantity }],
        PagoInicial = payment == 0 ? null : new PagoInicialVentaDto { Monto = payment, FormaPago = "Efectivo" }
    };

    public static CrearVentaDto Vehicle(SyntheticCase seed, int id, decimal payment = 0) => new()
    {
        ClienteId = seed.ClientId, Detalles = [new() { VehiculoId = id, Cantidad = 1 }],
        PagoInicial = payment == 0 ? null : new PagoInicialVentaDto { Monto = payment, FormaPago = "Efectivo" }
    };

    public static async Task<VentaDto> Create(TestingDatabase database, SyntheticCase buyer, CrearVentaDto request, params IInterceptor[] interceptors)
    {
        await using var db = await database.CreateContextAsync(interceptors);
        return await TestingDatabase.Sales(db).CrearAsync(request, buyer.UserId);
    }

    public static async Task<(VentaDto? Sale, string Code)> Attempt(TestingDatabase database, SyntheticCase buyer,
        CrearVentaDto request, params IInterceptor[] interceptors)
    {
        try { return (await Create(database, buyer, request, interceptors), "ok"); }
        catch (ReglaFinancieraException rule) when (rule.EstadoHttp == 409) { return (null, rule.Codigo); }
    }

    public static async Task AssertNoNewSaleOrMovements(TestingDatabase database, SyntheticCase seed)
    {
        await using var fresh = await database.CreateContextAsync();
        Assert.False(await fresh.Ventas.AnyAsync(v => v.ClienteId == seed.ClientId && v.Id != seed.SaleId));
        Assert.False(await fresh.DetallesVenta.AnyAsync(d => d.Venta.ClienteId == seed.ClientId && d.VentaId != seed.SaleId));
        Assert.False(await fresh.PagosVenta.AnyAsync(p => p.Venta.ClienteId == seed.ClientId));
        Assert.Equal(1, await fresh.CuentasCorrientes.CountAsync(c => c.ClienteId == seed.ClientId));
        Assert.Equal(1000m, await fresh.CuentasCorrientes.Where(c => c.ClienteId == seed.ClientId).SumAsync(c => c.Debe - c.Haber));
        Assert.Equal("Pendiente", (await fresh.Ventas.FindAsync(seed.SaleId))!.Estado);
    }

    public static async Task AssertRunningBalance(TestingDatabase database, int clientId, decimal expected)
    {
        await using var db = await database.CreateContextAsync();
        var rows = await db.CuentasCorrientes.Where(c => c.ClienteId == clientId).OrderBy(c => c.Id).ToListAsync();
        decimal balance = 0;
        foreach (var row in rows)
        { balance += row.Debe - row.Haber; Assert.Equal(balance, row.Saldo); }
        Assert.Equal(expected, balance);
        Assert.Equal(expected, (await TestingDatabase.Ledger(db).ObtenerResumenAsync()).Single(c => c.ClienteId == clientId).SaldoActual);
    }
}

internal sealed class AfterSaleSavedFailure : SaveChangesInterceptor
{
    public bool Triggered { get; private set; }
    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context!.ChangeTracker.Entries<Venta>().Any(e => e.Entity.Id > 0))
        { Triggered = true; throw new InjectedFailure(); }
        return ValueTask.FromResult(result);
    }
}

internal sealed class StockCommandFailure(bool after, bool vehicle = false, int ordinal = 1) : DbCommandInterceptor
{
    private int _calls;
    public bool Triggered { get; private set; }
    private bool Matches(DbCommand command) => command.CommandText.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
        && command.CommandText.Contains(vehicle ? "[Vehiculos]" : "[Productos]", StringComparison.Ordinal)
        && command.CommandText.Contains(vehicle ? "[Estado]" : "[Stock]", StringComparison.Ordinal);

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (!after && Matches(command) && ++_calls == ordinal) { Triggered = true; throw new InjectedFailure(); }
        return ValueTask.FromResult(result);
    }
    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
        int result, CancellationToken cancellationToken = default)
    {
        if (after && Matches(command) && ++_calls == ordinal) { Triggered = true; throw new InjectedFailure(); }
        return ValueTask.FromResult(result);
    }
}

internal sealed class PauseSaleWrite : SaveChangesInterceptor
{
    public int SessionId { get; private set; }
    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context!.ChangeTracker.Entries<Venta>().Any(e => e.State == EntityState.Added))
        {
            SessionId = ((SqlConnection)eventData.Context.Database.GetDbConnection()).ServerProcessId;
            Entered.TrySetResult();
            await Release.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        }
        return result;
    }
}

internal sealed class InventoryReadObserver : DbCommandInterceptor
{
    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public List<int> ProductIds { get; } = [];
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("UPDLOCK", StringComparison.Ordinal))
        {
            if (command.CommandText.Contains("[Productos]", StringComparison.Ordinal))
                ProductIds.Add(Convert.ToInt32(command.Parameters[0].Value));
            Entered.TrySetResult();
        }
        return ValueTask.FromResult(result);
    }
}
