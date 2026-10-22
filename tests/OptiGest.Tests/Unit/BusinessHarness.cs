using Moq;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services;
using SistemaDeVentas.DTOs.Financiaciones;
using SistemaDeVentas.DTOs.Ventas;

namespace OptiGest.Tests.Unit;

internal sealed class BusinessHarness
{
    public Mock<IVentaRepository> Sales { get; } = new();
    public Mock<IClienteRepository> Clients { get; } = new();
    public Mock<IUsuarioRepository> Users { get; } = new();
    public Mock<IProductoRepository> Products { get; } = new();
    public Mock<IVehiculoRepository> Vehicles { get; } = new();
    public Mock<IFinanciacionRepository> Financing { get; } = new();
    public Mock<IConfiguracionFinanciacionRepository> Plans { get; } = new();
    public Mock<ICuotaRepository> Installments { get; } = new();
    public Mock<ICuentaCorrienteRepository> Ledger { get; } = new();
    public Mock<IOperacionFinancieraRepository> Operation { get; } = new();
    public List<Venta> CreatedSales { get; } = [];
    public List<Financiacion> CreatedFinancing { get; } = [];
    public List<Cuota> CreatedInstallments { get; } = [];
    public Producto Product { get; } = new() { Id = 10, Nombre = "Synthetic helmet", Stock = 10, PrecioVenta = 100m };
    public Vehiculo Vehicle { get; } = new() { Id = 20, Estado = "Disponible", PrecioVenta = 1000m };
    public ConfiguracionFinanciacion Plan { get; } = new()
    { Id = 30, Activa = true, CantidadCuotas = 3, TasaInteres = 10m, MontoMinimo = 0, MontoMaximo = 10_000_000m };
    public Venta Sale { get; } = new() { Id = 40, ClienteId = 1, Estado = "Pendiente", Total = 1000m };

    public BusinessHarness()
    {
        Clients.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(new Cliente { Id = 1 });
        Users.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(new Usuario { Id = 1 });
        Products.Setup(r => r.ObtenerPorIdAsync(10)).ReturnsAsync(Product);
        Products.Setup(r => r.ObtenerParaVentaAsync(10)).ReturnsAsync(Product);
        Products.Setup(r => r.DescontarStockAsync(10, It.IsAny<int>()))
            .Callback((int _, int quantity) => Product.Stock -= quantity).Returns(Task.CompletedTask);
        Vehicles.Setup(r => r.ObtenerPorIdAsync(It.IsAny<int>())).ReturnsAsync((int id) =>
            id == 20 ? Vehicle : new Vehiculo { Id = id, Estado = "Disponible", PrecioVenta = 1000m });
        Vehicles.Setup(r => r.ObtenerParaVentaAsync(20)).ReturnsAsync(Vehicle);
        Vehicles.Setup(r => r.MarcarVendidoAsync(20)).Callback(() => Vehicle.Estado = "Vendido").Returns(Task.CompletedTask);
        Sales.Setup(r => r.ObtenerPorIdAsync(40)).ReturnsAsync(Sale);
        Sales.Setup(r => r.ObtenerClienteIdAsync(40)).ReturnsAsync(1);
        Operation.Setup(r => r.EjecutarAsync(It.IsAny<int>(), It.IsAny<Func<Task<FinanciacionDto>>>()))
            .Returns((int _, Func<Task<FinanciacionDto>> operation) => operation());
        Operation.Setup(r => r.EjecutarAsync(It.IsAny<int>(), It.IsAny<Func<Task<VentaDto>>>()))
            .Returns((int _, Func<Task<VentaDto>> operation) => operation());
        Plans.Setup(r => r.ObtenerPorIdAsync(30)).ReturnsAsync(Plan);
        Sales.Setup(r => r.CrearAsync(It.IsAny<Venta>())).ReturnsAsync((Venta value) =>
        { value.Id = 41; CreatedSales.Add(value); return value; });
        Financing.Setup(r => r.CrearAsync(It.IsAny<Financiacion>())).ReturnsAsync((Financiacion value) =>
        { value.Id = 50; CreatedFinancing.Add(value); return value; });
        Installments.Setup(r => r.CrearAsync(It.IsAny<Cuota>())).ReturnsAsync((Cuota value) =>
        { CreatedInstallments.Add(value); return value; });
        Ledger.Setup(r => r.CrearAsync(It.IsAny<CuentaCorriente>())).ReturnsAsync((CuentaCorriente value) => value);
        Sale.DetallesVenta.Add(new DetalleVenta { VehiculoId = Vehicle.Id, Cantidad = 1 });
    }

    public VentaService SaleService() => new(Sales.Object, Clients.Object, Products.Object, Ledger.Object, Vehicles.Object, Operation.Object, Users.Object);
    public FinanciacionService FinancingService() => new(Financing.Object, Sales.Object, Plans.Object, Installments.Object, Ledger.Object, Operation.Object);
}
