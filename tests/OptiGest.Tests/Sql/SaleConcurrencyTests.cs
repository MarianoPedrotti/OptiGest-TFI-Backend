using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.DTOs.Financiaciones;
using SistemaDeVentas.DTOs.PagosVenta;
using SistemaDeVentas.DTOs.Ventas;
using static OptiGest.Tests.Sql.SaleTestingSupport;

namespace OptiGest.Tests.Sql;

[Collection("TestingSql"), Trait("Suite", "Sql")]
public class SaleConcurrencyTests(TestingDatabase database)
{
    private async Task AssertInventoryLockHeld(int session, string table)
    {
        await using var probe = await database.CreateContextAsync();
        var count = await probe.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS [Value] FROM sys.dm_tran_locks AS l
            INNER JOIN sys.partitions AS p ON p.hobt_id = l.resource_associated_entity_id
            WHERE l.request_session_id = {session} AND l.resource_database_id = DB_ID()
              AND l.request_status = 'GRANT' AND l.resource_type = 'KEY'
              AND l.request_mode IN ('U', 'X', 'RangeS-U', 'RangeX-X')
              AND OBJECT_NAME(p.object_id) = {table}
            """).SingleAsync();
        Assert.True(count > 0, $"No inventory update lock held on {table}");
    }

    [SqlFact]
    public async Task Two_clients_cannot_oversell_one_product_even_before_first_header_is_written()
    {
        var a = await Seed(database);
        var b = await Seed(database);
        var pause = new PauseSaleWrite();
        var observer = new InventoryReadObserver();
        var first = Attempt(database, a, Articles(a, a.ProductId, 6), pause);
        Task<(VentaDto? Sale, string Code)> second;
        try
        {
            await pause.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await AssertInventoryLockHeld(pause.SessionId, "Productos");
            second = Attempt(database, b, Articles(b, a.ProductId, 6), observer);
            await observer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(second.IsCompleted);
        }
        finally { pause.Release.TrySetResult(); }
        var results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.Equal("ok", results[0].Code);
        Assert.Equal("stock_insuficiente", results[1].Code);
        await using var db = await database.CreateContextAsync();
        Assert.Equal(4, (await db.Productos.FindAsync(a.ProductId))!.Stock);
        Assert.Equal(1, await db.DetallesVenta.CountAsync(d => d.ProductoId == a.ProductId));
        await AssertNoNewSaleOrMovements(database, b);
        await AssertRunningBalance(database, a.ClientId, 1600m);
    }

    [SqlFact]
    public async Task Shared_product_can_be_sold_to_two_clients_when_accumulated_stock_is_sufficient()
    {
        var a = await Seed(database);
        var b = await Seed(database);
        var results = await Task.WhenAll(Create(database, a, Articles(a, a.ProductId, 4)), Create(database, b, Articles(b, a.ProductId, 6)));
        Assert.NotEqual(results[0].Id, results[1].Id);
        await using var db = await database.CreateContextAsync();
        Assert.Equal(0, (await db.Productos.FindAsync(a.ProductId))!.Stock);
        Assert.Equal(10, await db.DetallesVenta.Where(d => d.ProductoId == a.ProductId).SumAsync(d => d.Cantidad));
        await AssertRunningBalance(database, a.ClientId, 1400m);
        await AssertRunningBalance(database, b.ClientId, 1600m);
    }

    [SqlFact]
    public async Task Two_distinct_users_and_clients_cannot_sell_the_same_vehicle()
    {
        var a = await Seed(database);
        var b = await Seed(database);
        Assert.NotEqual(a.UserId, b.UserId);
        var vehicle = await FreshVehicle(database, a);
        var pause = new PauseSaleWrite();
        var observer = new InventoryReadObserver();
        var first = Attempt(database, a, Vehicle(a, vehicle), pause);
        Task<(VentaDto? Sale, string Code)> second;
        try
        {
            await pause.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await AssertInventoryLockHeld(pause.SessionId, "Vehiculos");
            second = Attempt(database, b, Vehicle(b, vehicle), observer);
            await observer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(second.IsCompleted);
        }
        finally { pause.Release.TrySetResult(); }
        var results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.Equal("ok", results[0].Code);
        Assert.Equal("vehiculo_no_disponible", results[1].Code);
        await using var db = await database.CreateContextAsync();
        Assert.Equal("Vendido", (await db.Vehiculos.FindAsync(vehicle))!.Estado);
        Assert.Equal(1, await db.DetallesVenta.CountAsync(d => d.VehiculoId == vehicle));
        Assert.Equal(a.UserId, (await db.Ventas.FindAsync(results[0].Sale!.Id))!.UsuarioId);
        await AssertNoNewSaleOrMovements(database, b);
    }

    [SqlFact]
    public async Task Opposite_cart_order_acquires_product_keys_ascending_and_both_sales_complete()
    {
        var a = await Seed(database);
        var b = await Seed(database);
        var one = Articles(a, b.ProductId, 1);
        one.Detalles.Add(new() { ProductoId = a.ProductId, Cantidad = 1 });
        var two = Articles(b, a.ProductId, 1);
        two.Detalles.Add(new() { ProductoId = b.ProductId, Cantidad = 1 });
        var observerA = new InventoryReadObserver();
        var observerB = new InventoryReadObserver();
        await Task.WhenAll(Create(database, a, one, observerA), Create(database, b, two, observerB)).WaitAsync(TimeSpan.FromSeconds(20));
        var sorted = new[] { a.ProductId, b.ProductId }.OrderBy(id => id).ToArray();
        Assert.Equal(sorted, observerA.ProductIds);
        Assert.Equal(sorted, observerB.ProductIds);
        await using var db = await database.CreateContextAsync();
        Assert.Equal(8, (await db.Productos.FindAsync(a.ProductId))!.Stock);
        Assert.Equal(8, (await db.Productos.FindAsync(b.ProductId))!.Stock);
    }

    [SqlFact]
    public async Task Independent_client_and_product_finish_while_first_sale_is_paused()
    {
        var a = await Seed(database);
        var b = await Seed(database);
        var pause = new PauseSaleWrite();
        var first = Create(database, a, Articles(a, a.ProductId, 1), pause);
        try
        {
            await pause.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var second = await Create(database, b, Articles(b, b.ProductId, 1)).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(second.Id > 0);
            Assert.False(first.IsCompleted);
        }
        finally { pause.Release.TrySetResult(); }
        await first.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [SqlFact]
    public async Task Distinct_products_for_one_client_keep_account_prefix_balances_consistent()
    {
        var a = await Seed(database);
        var b = await Seed(database);
        await Task.WhenAll(Create(database, a, Articles(a, a.ProductId, 1)), Create(database, a, Articles(a, b.ProductId, 2)));
        await AssertRunningBalance(database, a.ClientId, 1300m);
        await using var db = await database.CreateContextAsync();
        Assert.Equal(9, (await db.Productos.FindAsync(a.ProductId))!.Stock);
        Assert.Equal(8, (await db.Productos.FindAsync(b.ProductId))!.Stock);
        Assert.Equal(3, await db.Ventas.CountAsync(v => v.ClienteId == a.ClientId));
    }

    [SqlTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sale_and_financial_operation_for_same_client_obey_phase_one_protocol(bool financing)
    {
        var seed = await Seed(database);
        async Task Financial()
        {
            await using var db = await database.CreateContextAsync();
            if (financing)
                await TestingDatabase.Financing(db).CrearAsync(new CrearFinanciacionDto
                { VentaId = seed.SaleId, ConfiguracionFinanciacionId = seed.PlanId });
            else
                await TestingDatabase.Payments(db).CrearAsync(new PagoVentaCreateDto
                { VentaId = seed.SaleId, Monto = 100m, FormaPago = "Efectivo" });
        }
        await Task.WhenAll(Create(database, seed, Articles(seed, seed.ProductId, 2, 50m)), Financial());
        await AssertRunningBalance(database, seed.ClientId, financing ? 1250m : 1050m);
        await using var db = await database.CreateContextAsync();
        Assert.Equal(8, (await db.Productos.FindAsync(seed.ProductId))!.Stock);
        Assert.Equal(2, await db.Ventas.CountAsync(v => v.ClienteId == seed.ClientId));
    }
}
