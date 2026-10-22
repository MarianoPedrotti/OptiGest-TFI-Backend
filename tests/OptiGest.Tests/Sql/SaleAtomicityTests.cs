using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SistemaDeVentas.DTOs.Ventas;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Helpers;
using SistemaDeVentas.Repositories;
using static OptiGest.Tests.Sql.SaleTestingSupport;

namespace OptiGest.Tests.Sql;

[Collection("TestingSql"), Trait("Suite", "Sql")]
public class SaleAtomicityTests(TestingDatabase database)
{
    [SqlTheory]
    [InlineData(0, "Pendiente")]
    [InlineData(50, "PagoParcial")]
    [InlineData(500, "Pagada")]
    public async Task Multiple_articles_are_consolidated_and_all_effects_commit_together(int initial, string state)
    {
        var a = await Seed(database);
        var b = await Seed(database);
        var request = Articles(a, a.ProductId, 2, initial);
        request.Detalles.Add(new DetalleVentaDto { ProductoId = a.ProductId, Cantidad = 1 });
        request.Detalles.Add(new DetalleVentaDto { ProductoId = b.ProductId, Cantidad = 2 });
        var result = await Create(database, a, request);
        await using var db = await database.CreateContextAsync();
        var sale = await db.Ventas.Include(v => v.DetallesVenta).Include(v => v.Pagos).SingleAsync(v => v.Id == result.Id);
        Assert.Equal(500m, sale.Total);
        Assert.Equal(state, sale.Estado);
        Assert.Equal(2, sale.DetallesVenta.Count);
        Assert.Equal(3, sale.DetallesVenta.Single(d => d.ProductoId == a.ProductId).Cantidad);
        Assert.All(sale.DetallesVenta, d => Assert.Equal(100m, d.PrecioUnitario));
        Assert.Equal(initial, sale.Pagos.Sum(p => p.Monto));
        Assert.Equal(7, (await db.Productos.FindAsync(a.ProductId))!.Stock);
        Assert.Equal(8, (await db.Productos.FindAsync(b.ProductId))!.Stock);
        await AssertRunningBalance(database, a.ClientId, 1500m - initial);
        Assert.Equal(1000m, (await db.Ventas.FindAsync(a.SaleId))!.Total); // Historical seed untouched.
    }

    [SqlFact]
    public async Task One_vehicle_is_sold_and_other_stock_is_not_changed()
    {
        var seed = await Seed(database);
        var vehicleId = await FreshVehicle(database, seed);
        var result = await Create(database, seed, Vehicle(seed, vehicleId, 1000m));
        await using var db = await database.CreateContextAsync();
        var detail = await db.DetallesVenta.SingleAsync(d => d.VentaId == result.Id);
        Assert.Equal(vehicleId, detail.VehiculoId);
        Assert.Null(detail.ProductoId);
        Assert.Equal(1, detail.Cantidad);
        Assert.Equal("Vendido", (await db.Vehiculos.FindAsync(vehicleId))!.Estado);
        Assert.Equal(10, (await db.Productos.FindAsync(seed.ProductId))!.Stock);
        Assert.Equal("Pagada", (await db.Ventas.FindAsync(result.Id))!.Estado);
        await AssertRunningBalance(database, seed.ClientId, 1000m);
    }

    [SqlTheory]
    [InlineData("mixed")]
    [InlineData("twoVehicles")]
    [InlineData("duplicateVehicle")]
    [InlineData("vehicleQuantity")]
    [InlineData("bothIds")]
    [InlineData("missingIds")]
    [InlineData("negativeQuantity")]
    public async Task Invalid_composition_has_no_persistent_effects(string scenario)
    {
        var seed = await Seed(database);
        var vehicle = await FreshVehicle(database, seed);
        var request = Vehicle(seed, vehicle);
        switch (scenario)
        {
            case "mixed": request.Detalles.Add(new() { ProductoId = seed.ProductId, Cantidad = 1 }); break;
            case "twoVehicles": request.Detalles.Add(new() { VehiculoId = await FreshVehicle(database, seed), Cantidad = 1 }); break;
            case "duplicateVehicle": request.Detalles.Add(new() { VehiculoId = vehicle, Cantidad = 1 }); break;
            case "vehicleQuantity": request.Detalles[0].Cantidad = 2; break;
            case "bothIds": request.Detalles[0].ProductoId = seed.ProductId; break;
            case "missingIds": request.Detalles[0].VehiculoId = null; break;
            case "negativeQuantity": request.Detalles[0].Cantidad = -1; break;
        }
        var error = await Assert.ThrowsAsync<ReglaFinancieraException>(() => Create(database, seed, request));
        Assert.Equal(400, error.EstadoHttp);
        await AssertNoNewSaleOrMovements(database, seed);
        await using var db = await database.CreateContextAsync();
        Assert.Equal("Disponible", (await db.Vehiculos.FindAsync(vehicle))!.Estado);
        Assert.Equal(10, (await db.Productos.FindAsync(seed.ProductId))!.Stock);
    }

    [SqlFact]
    public async Task Accumulated_stock_is_checked_before_any_sale_is_persisted()
    {
        var seed = await Seed(database);
        var request = Articles(seed, seed.ProductId, 6, 50m);
        request.Detalles.Add(new() { ProductoId = seed.ProductId, Cantidad = 6 });
        var error = await Assert.ThrowsAsync<ReglaFinancieraException>(() => Create(database, seed, request));
        Assert.Equal("stock_insuficiente", error.Codigo);
        await AssertNoNewSaleOrMovements(database, seed);
        await using var db = await database.CreateContextAsync();
        Assert.Equal(10, (await db.Productos.FindAsync(seed.ProductId))!.Stock);
    }

    [SqlTheory]
    [InlineData("missing")]
    [InlineData("price")]
    [InlineData("stock")]
    public async Task Invalid_last_item_does_not_persist_or_consume_previously_validated_items(string scenario)
    {
        var a = await Seed(database);
        var b = await Seed(database);
        await using (var setup = await database.CreateContextAsync())
        {
            if (scenario == "price") (await setup.Productos.FindAsync(b.ProductId))!.PrecioVenta = 0;
            if (scenario == "stock") (await setup.Productos.FindAsync(b.ProductId))!.Stock = 0;
            await setup.SaveChangesAsync();
        }
        var request = Articles(a, a.ProductId, 1);
        request.Detalles.Add(new() { ProductoId = scenario == "missing" ? int.MaxValue : b.ProductId, Cantidad = 1 });
        var error = await Assert.ThrowsAsync<ReglaFinancieraException>(() => Create(database, a, request));
        Assert.Equal(scenario switch { "missing" => "producto_inexistente", "price" => "precio_invalido", _ => "stock_insuficiente" }, error.Codigo);
        await AssertNoNewSaleOrMovements(database, a);
        await using var fresh = await database.CreateContextAsync();
        Assert.Equal(10, (await fresh.Productos.FindAsync(a.ProductId))!.Stock);
    }

    [SqlTheory]
    [InlineData("Vendido")]
    [InlineData("Reservado")]
    public async Task Unavailable_vehicle_is_rejected_without_sale(string state)
    {
        var seed = await Seed(database);
        var vehicle = await FreshVehicle(database, seed, state);
        var error = await Assert.ThrowsAsync<ReglaFinancieraException>(() => Create(database, seed, Vehicle(seed, vehicle)));
        Assert.Equal("vehiculo_no_disponible", error.Codigo);
        await AssertNoNewSaleOrMovements(database, seed);
    }

    [SqlTheory]
    [InlineData("afterHeader")]
    [InlineData("beforeStock")]
    [InlineData("afterFirstStock")]
    [InlineData("afterSecondStock")]
    [InlineData("debit")]
    [InlineData("credit")]
    public async Task Failure_at_any_stage_rolls_back_sale_details_payment_stock_and_account(string stage)
    {
        var a = await Seed(database);
        var b = await Seed(database);
        var request = Articles(a, a.ProductId, 2, 50m);
        request.Detalles.Add(new() { ProductoId = b.ProductId, Cantidad = 3 });
        var header = new AfterSaleSavedFailure();
        var stock = new StockCommandFailure(stage != "beforeStock", ordinal: stage == "afterSecondStock" ? 2 : 1);
        var ledger = new FailOnWrite(db => db.ChangeTracker.Entries<CuentaCorriente>()
            .Any(e => e.State == EntityState.Added && (stage == "debit" ? e.Entity.Debe > 0 : e.Entity.Haber > 0)));
        IInterceptor fault = stage == "afterHeader" ? header : stage is "debit" or "credit" ? ledger : stock;
        await Assert.ThrowsAsync<InjectedFailure>(() => Create(database, a, request, fault));
        Assert.True(stage == "afterHeader" ? header.Triggered : stage is "debit" or "credit" ? ledger.Triggered : stock.Triggered);
        await AssertNoNewSaleOrMovements(database, a);
        await using var fresh = await database.CreateContextAsync();
        Assert.Equal(10, (await fresh.Productos.FindAsync(a.ProductId))!.Stock);
        Assert.Equal(10, (await fresh.Productos.FindAsync(b.ProductId))!.Stock);
    }

    [SqlFact]
    public async Task Failure_after_vehicle_update_reverts_availability_and_sale()
    {
        var seed = await Seed(database);
        var vehicle = await FreshVehicle(database, seed);
        var fault = new StockCommandFailure(after: true, vehicle: true);
        await Assert.ThrowsAsync<InjectedFailure>(() => Create(database, seed, Vehicle(seed, vehicle, 50m), fault));
        Assert.True(fault.Triggered);
        await AssertNoNewSaleOrMovements(database, seed);
        await using var db = await database.CreateContextAsync();
        Assert.Equal("Disponible", (await db.Vehiculos.FindAsync(vehicle))!.Estado);
    }

    [SqlFact]
    public async Task Conditional_repository_updates_refuse_negative_stock_or_second_vehicle_sale()
    {
        var seed = await Seed(database);
        await using var db = await database.CreateContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        await Assert.ThrowsAsync<ReglaFinancieraException>(() => new ProductoRepository(db).DescontarStockAsync(seed.ProductId, 11));
        await Assert.ThrowsAsync<ReglaFinancieraException>(() => new VehiculoRepository(db).MarcarVendidoAsync(seed.VehicleId));
        await tx.RollbackAsync();
        await using var fresh = await database.CreateContextAsync();
        Assert.Equal(10, (await fresh.Productos.FindAsync(seed.ProductId))!.Stock);
        Assert.Equal("Vendido", (await fresh.Vehiculos.FindAsync(seed.VehicleId))!.Estado);
    }

    private sealed class CommitAcknowledgementFailure : DbTransactionInterceptor
    {
        public int Calls { get; private set; }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        { Calls++; throw new IOException("Synthetic lost acknowledgement AFTER sale commit"); }
    }

    [SqlFact]
    public async Task Unknown_sale_commit_is_not_replayed_and_has_a_complete_aggregate()
    {
        var seed = await Seed(database);
        var fault = new CommitAcknowledgementFailure();
        await Assert.ThrowsAsync<ResultadoFinancieroDesconocidoException>(() => Create(database, seed, Articles(seed, seed.ProductId, 2, 50m), fault));
        Assert.Equal(1, fault.Calls);
        await using var db = await database.CreateContextAsync();
        var sale = await db.Ventas.Include(v => v.DetallesVenta).Include(v => v.Pagos)
            .SingleAsync(v => v.ClienteId == seed.ClientId && v.Id != seed.SaleId);
        Assert.Single(sale.DetallesVenta);
        Assert.Equal(50m, Assert.Single(sale.Pagos).Monto);
        Assert.Equal(8, (await db.Productos.FindAsync(seed.ProductId))!.Stock);
        await AssertRunningBalance(database, seed.ClientId, 1150m);
    }
}
