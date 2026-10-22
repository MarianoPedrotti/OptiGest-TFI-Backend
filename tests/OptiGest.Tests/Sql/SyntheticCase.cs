using SistemaDeVentas.Data;
using SistemaDeVentas.Entities;

namespace OptiGest.Tests.Sql;

internal sealed record SyntheticCase(int UserId, int ClientId, int ProductId, int VehicleId, int PlanId, int SaleId)
{
    public static async Task<SyntheticCase> SeedAsync(AppDbContext db)
    {
        var id = Guid.NewGuid().ToString("N");
        var user = new Usuario { Nombre = "Synthetic", Apellido = "Testing", Email = $"{id}@example.invalid", PasswordHash = "NOT-A-REAL-PASSWORD", Role = "Admin" };
        var client = new Cliente { Nombre = "Synthetic client", Dni = id, Email = $"client-{id}@example.invalid", Telefono = "000", Direccion = "Testing", Usuario = user };
        var product = new Producto
        {
            Nombre = "Synthetic helmet", SKU = id, Stock = 10, PrecioVenta = 100m,
            MarcaProducto = new MarcaProducto { Nombre = id },
            SubCategoria = new SubCategoria { Nombre = id, Categoria = new Categoria { Nombre = id } }
        };
        var vehicle = new Vehiculo
        {
            VIN = id, NumeroMotor = id, Patente = id, Estado = "Vendido", PrecioVenta = 1000m,
            Modelo = new Modelo { Nombre = id, Cilindrada = 125, TipoMotor = "Testing", MarcaVehiculo = new MarcaVehiculo { Nombre = id } }
        };
        var plan = new ConfiguracionFinanciacion { Activa = true, CantidadCuotas = 3, TasaInteres = 10m, MontoMinimo = 0, MontoMaximo = 10_000_000m };
        var sale = new Venta { Cliente = client, Usuario = user, Total = 1000m, Subtotal = 1000m, Estado = "Pendiente", Fecha = DateTime.Now };
        sale.DetallesVenta.Add(new DetalleVenta { Vehiculo = vehicle, PrecioUnitario = 1000m, Cantidad = 1 });
        db.AddRange(product, plan, sale);
        db.CuentasCorrientes.Add(new CuentaCorriente
        { Cliente = client, Fecha = sale.Fecha, Concepto = "Testing seed sale", Debe = 1000m, Haber = 0, Saldo = 1000m });
        await db.SaveChangesAsync();
        return new(user.Id, client.Id, product.Id, vehicle.Id, plan.Id, sale.Id);
    }
}
