using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using static OptiGest.Tests.Sql.SaleTestingSupport;

namespace OptiGest.Tests.Sql;

[Collection("TestingSql"), Trait("Suite", "Sql")]
public class SaleHttpTests(TestingDatabase database)
{
    [SqlFact]
    public async Task Http_sale_ignores_untrusted_prices_and_returns_only_after_committed_consolidation()
    {
        var seed = await Seed(database);
        using var server = await FinancialHttpTests.CreateServer(database);
        using var client = server.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/Venta", new
        {
            clienteId = seed.ClientId,
            detalles = new[]
            {
                new { productoId = seed.ProductId, cantidad = 2, precioUnitario = 0.01m },
                new { productoId = seed.ProductId, cantidad = 3, precioUnitario = 0.01m }
            }
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(500m, json.GetProperty("total").GetDecimal());
        var id = json.GetProperty("id").GetInt32();
        await using var db = await database.CreateContextAsync();
        var detail = await db.DetallesVenta.SingleAsync(d => d.VentaId == id);
        Assert.Equal(5, detail.Cantidad);
        Assert.Equal(100m, detail.PrecioUnitario);
        Assert.Equal(5, (await db.Productos.FindAsync(seed.ProductId))!.Stock);
        await AssertRunningBalance(database, seed.ClientId, 1500m);
    }

    [SqlFact]
    public async Task Http_mixed_sale_returns_400_and_stock_conflict_returns_409_without_side_effects()
    {
        var seed = await Seed(database);
        var vehicle = await FreshVehicle(database, seed);
        using var server = await FinancialHttpTests.CreateServer(database);
        using var client = server.CreateClient();
        var mixed = Articles(seed, seed.ProductId, 1);
        mixed.Detalles.Add(new() { VehiculoId = vehicle, Cantidad = 1 });
        using var bad = await client.PostAsJsonAsync("/api/Venta", mixed);
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal("venta_mixta", (await bad.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        using var conflict = await client.PostAsJsonAsync("/api/Venta", Articles(seed, seed.ProductId, 11));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("stock_insuficiente", (await conflict.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await AssertNoNewSaleOrMovements(database, seed);
    }
}
