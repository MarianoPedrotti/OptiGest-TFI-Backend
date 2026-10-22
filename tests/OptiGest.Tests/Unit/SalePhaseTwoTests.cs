using Moq;
using SistemaDeVentas.DTOs.Ventas;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Helpers;

namespace OptiGest.Tests.Unit;

[Trait("Suite", "Unit")]
public class SalePhaseTwoTests
{
    private static CrearVentaDto Request(params DetalleVentaDto[] details) => new() { ClienteId = 1, Detalles = [.. details] };

    [Fact]
    public async Task Vehicle_sale_uses_stored_price_and_sells_one_unit()
    {
        var h = new BusinessHarness();
        var request = Request(new DetalleVentaDto { VehiculoId = 20, Cantidad = 1 });
        request.PagoInicial = new PagoInicialVentaDto { Monto = 1000m, FormaPago = "Efectivo" };
        var result = await h.SaleService().CrearAsync(request, 1);
        Assert.Equal(1000m, result.Total);
        Assert.True(result.TieneVehiculo);
        Assert.True(result.EstaPagada);
        Assert.Equal(0m, result.SaldoPendiente);
        Assert.Equal("Vendido", h.Vehicle.Estado);
        Assert.Equal(10, h.Product.Stock);
        h.Vehicles.Verify(r => r.MarcarVendidoAsync(20), Times.Once);
        var detail = Assert.Single(Assert.Single(h.CreatedSales).DetallesVenta);
        Assert.Equal(1, detail.Cantidad);
        Assert.Equal(1000m, detail.PrecioUnitario);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(2)]
    public async Task Vehicle_quantity_must_be_exactly_one_before_any_lookup_or_write(int quantity)
    {
        var h = new BusinessHarness();
        await Assert.ThrowsAsync<ReglaFinancieraException>(() => h.SaleService().CrearAsync(Request(new DetalleVentaDto { VehiculoId = 20, Cantidad = quantity }), 1));
        Assert.Empty(h.CreatedSales);
        h.Vehicles.Verify(r => r.ObtenerParaVentaAsync(It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(null, -1)]
    [InlineData(-10, null)]
    public async Task Item_identifiers_must_be_positive(int? product, int? vehicle)
    {
        var h = new BusinessHarness();
        var error = await Assert.ThrowsAsync<ReglaFinancieraException>(() => h.SaleService().CrearAsync(
            Request(new DetalleVentaDto { ProductoId = product, VehiculoId = vehicle, Cantidad = 1 }), 1));
        Assert.Equal(400, error.EstadoHttp);
        Assert.Empty(h.CreatedSales);
    }

    [Fact]
    public async Task Null_detail_is_rejected_as_invalid_input()
    {
        var h = new BusinessHarness();
        await Assert.ThrowsAsync<ReglaFinancieraException>(() => h.SaleService().CrearAsync(Request(new DetalleVentaDto[] { null! }), 1));
        Assert.Empty(h.CreatedSales);
    }

    [Fact]
    public async Task Empty_sale_is_rejected_before_starting_transaction()
    {
        var h = new BusinessHarness();
        await Assert.ThrowsAsync<ReglaFinancieraException>(() => h.SaleService().CrearAsync(Request(), 1));
        h.Operation.Verify(r => r.EjecutarAsync(It.IsAny<int>(), It.IsAny<Func<Task<VentaDto>>>()), Times.Never);
    }

    [Fact]
    public async Task Accumulated_quantity_overflow_is_a_controlled_rejection()
    {
        var h = new BusinessHarness();
        var error = await Assert.ThrowsAsync<ReglaFinancieraException>(() => h.SaleService().CrearAsync(Request(
            new() { ProductoId = 10, Cantidad = int.MaxValue }, new() { ProductoId = 10, Cantidad = 1 }), 1));
        Assert.Equal("cantidad_fuera_de_rango", error.Codigo);
        Assert.Empty(h.CreatedSales);
        h.Products.Verify(r => r.ObtenerParaVentaAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Negative_quantity_cannot_cancel_positive_duplicates()
    {
        var h = new BusinessHarness();
        await Assert.ThrowsAsync<ReglaFinancieraException>(() => h.SaleService().CrearAsync(Request(
            new() { ProductoId = 10, Cantidad = 12 }, new() { ProductoId = 10, Cantidad = -4 }), 1));
        Assert.Empty(h.CreatedSales);
        Assert.Equal(10, h.Product.Stock);
    }

    [Fact]
    public async Task Consolidation_keeps_original_request_and_acquires_keys_in_ascending_order()
    {
        var h = new BusinessHarness();
        var second = new Producto { Id = 11, Nombre = "Synthetic second item", Stock = 10, PrecioVenta = 25.50m };
        var reads = new List<int>();
        h.Products.Setup(r => r.ObtenerParaVentaAsync(It.IsAny<int>())).ReturnsAsync((int id) =>
        { reads.Add(id); return id == 10 ? h.Product : second; });
        var request = Request(new() { ProductoId = 11, Cantidad = 2 }, new() { ProductoId = 10, Cantidad = 2 }, new() { ProductoId = 10, Cantidad = 3 });
        var result = await h.SaleService().CrearAsync(request, 1);
        Assert.Equal(new[] { 10, 11 }, reads);
        Assert.Equal(551m, result.Total);
        Assert.Equal(new[] { 2, 2, 3 }, request.Detalles.Select(d => d.Cantidad));
        var stored = Assert.Single(h.CreatedSales).DetallesVenta.ToList();
        Assert.Equal(2, stored.Count);
        Assert.Equal(5, stored.Single(d => d.ProductoId == 10).Cantidad);
        h.Products.Verify(r => r.DescontarStockAsync(10, 5), Times.Once);
        h.Products.Verify(r => r.DescontarStockAsync(11, 2), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100.001)]
    public async Task Invalid_stored_price_is_rejected_before_persistence(double price)
    {
        var h = new BusinessHarness();
        h.Product.PrecioVenta = (decimal)price;
        var error = await Assert.ThrowsAsync<ReglaFinancieraException>(() => h.SaleService().CrearAsync(Request(new DetalleVentaDto { ProductoId = 10, Cantidad = 1 }), 1));
        Assert.Equal("precio_invalido", error.Codigo);
        Assert.Empty(h.CreatedSales);
        h.Products.Verify(r => r.DescontarStockAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Total_is_checked_against_sql_monetary_precision_before_writing()
    {
        var h = new BusinessHarness();
        h.Product.PrecioVenta = 9999999999999999.99m;
        var error = await Assert.ThrowsAsync<ReglaFinancieraException>(() => h.SaleService().CrearAsync(Request(new DetalleVentaDto { ProductoId = 10, Cantidad = 2 }), 1));
        Assert.Equal("total_fuera_de_rango", error.Codigo);
        Assert.Empty(h.CreatedSales);
    }

    [Theory]
    [InlineData(-1, "Efectivo")]
    [InlineData(201, "Efectivo")]
    [InlineData(50.001, "Efectivo")]
    [InlineData(50, "")]
    public async Task Initial_payment_is_validated_before_sale_and_stock(double payment, string method)
    {
        var h = new BusinessHarness();
        var request = Request(new DetalleVentaDto { ProductoId = 10, Cantidad = 2 });
        request.PagoInicial = new PagoInicialVentaDto { Monto = (decimal)payment, FormaPago = method };
        await Assert.ThrowsAsync<ReglaFinancieraException>(() => h.SaleService().CrearAsync(request, 1));
        Assert.Empty(h.CreatedSales);
        Assert.Equal(10, h.Product.Stock);
        h.Ledger.Verify(r => r.CrearAsync(It.IsAny<CuentaCorriente>()), Times.Never);
    }

    [Theory]
    [InlineData("client")]
    [InlineData("user")]
    [InlineData("product")]
    [InlineData("vehicle")]
    public async Task Missing_references_are_rejected_without_sale(string reference)
    {
        var h = new BusinessHarness();
        if (reference == "client") h.Clients.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync((Cliente?)null);
        if (reference == "user") h.Users.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync((Usuario?)null);
        if (reference == "product") h.Products.Setup(r => r.ObtenerParaVentaAsync(10)).ReturnsAsync((Producto?)null);
        if (reference == "vehicle") h.Vehicles.Setup(r => r.ObtenerParaVentaAsync(20)).ReturnsAsync((Vehiculo?)null);
        var detail = reference == "vehicle" ? new DetalleVentaDto { VehiculoId = 20, Cantidad = 1 } : new DetalleVentaDto { ProductoId = 10, Cantidad = 1 };
        var error = await Assert.ThrowsAsync<ReglaFinancieraException>(() => h.SaleService().CrearAsync(Request(detail), 1));
        Assert.Equal(404, error.EstadoHttp);
        Assert.Empty(h.CreatedSales);
    }
}
