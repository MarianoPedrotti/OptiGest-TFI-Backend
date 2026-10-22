using SistemaDeVentas.DTOs.Ventas;

namespace OptiGest.Tests.Unit;

[Trait("Suite", "Unit")]
public class SaleTests
{
    private static CrearVentaDto Request(params DetalleVentaDto[] details) => new() { ClienteId = 1, Detalles = [.. details] };
    private static DetalleVentaDto Product(int quantity) => new() { ProductoId = 10, Cantidad = quantity };

    [Theory]
    [InlineData(0, "Pendiente")]
    [InlineData(50, "PagoParcial")]
    [InlineData(200, "Pagada")]
    public async Task Initial_payment_sets_current_sale_state(int payment, string expected)
    {
        var h = new BusinessHarness();
        var request = Request(Product(2));
        if (payment > 0) request.PagoInicial = new PagoInicialVentaDto { Monto = payment, FormaPago = "Efectivo" };
        var result = await h.SaleService().CrearAsync(request, 1);
        Assert.Equal(expected, result.Estado);
        Assert.Equal(200m, result.Total);
        Assert.Equal(8, h.Product.Stock);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task Invalid_product_quantity_is_rejected_without_sale(int quantity)
    {
        var h = new BusinessHarness();
        await Assert.ThrowsAnyAsync<Exception>(() => h.SaleService().CrearAsync(Request(Product(quantity)), 1));
        Assert.Empty(h.CreatedSales);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Detail_requires_exactly_one_item_kind(bool both)
    {
        var h = new BusinessHarness();
        var detail = both ? new DetalleVentaDto { ProductoId = 10, VehiculoId = 20, Cantidad = 1 } : new DetalleVentaDto { Cantidad = 1 };
        await Assert.ThrowsAnyAsync<Exception>(() => h.SaleService().CrearAsync(Request(detail), 1));
        Assert.Empty(h.CreatedSales);
    }

    [Fact]
    public async Task New_rule_rejects_mixed_sale()
    {
        var h = new BusinessHarness();
        await Assert.ThrowsAnyAsync<Exception>(() => h.SaleService().CrearAsync(
            Request(Product(1), new DetalleVentaDto { VehiculoId = 20, Cantidad = 1 }), 1));
        Assert.Empty(h.CreatedSales);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(21)]
    public async Task New_rule_rejects_second_vehicle_even_when_repeated(int secondId)
    {
        var h = new BusinessHarness();
        await Assert.ThrowsAnyAsync<Exception>(() => h.SaleService().CrearAsync(Request(
            new DetalleVentaDto { VehiculoId = 20, Cantidad = 1 },
            new DetalleVentaDto { VehiculoId = secondId, Cantidad = 1 }), 1));
    }

    [Fact]
    public async Task New_rule_consolidates_duplicate_products_before_persistence()
    {
        var h = new BusinessHarness();
        await h.SaleService().CrearAsync(Request(Product(2), Product(3)), 1);
        var detail = Assert.Single(Assert.Single(h.CreatedSales).DetallesVenta);
        Assert.Equal(5, detail.Cantidad);
    }

    [Fact]
    public async Task New_rule_rejects_accumulated_quantity_above_stock()
    {
        var h = new BusinessHarness();
        await Assert.ThrowsAnyAsync<Exception>(() => h.SaleService().CrearAsync(Request(Product(6), Product(6)), 1));
        Assert.Empty(h.CreatedSales);
        Assert.Equal(10, h.Product.Stock);
    }
}
