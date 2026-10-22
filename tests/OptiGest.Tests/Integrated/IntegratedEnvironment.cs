using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OptiGest.Tests.Sql;
using SistemaDeVentas.Data;
using SistemaDeVentas.Entities;

namespace OptiGest.Tests.Integrated;

public sealed class E2EUtilityFactAttribute : FactAttribute
{
    public E2EUtilityFactAttribute(string optIn)
    {
        if (Environment.GetEnvironmentVariable(optIn) != "1")
            Skip = "Explicit E2E utility opt-in required: " + optIn;
    }
}

// Explicit utilities; normal regression reports them as skipped, not business tests.
[Trait("Suite", "E2EPreparation")]
public class IntegratedEnvironment
{
    [E2EUtilityFact("OPTIGEST_PREPARE_E2E")]
    public async Task Prepare_new_owned_database()
    {
        if (Environment.GetEnvironmentVariable("OPTIGEST_PREPARE_E2E") != "1")
            throw new InvalidOperationException("Explicit E2E preparation opt-in required.");
        var root = Environment.GetEnvironmentVariable("OPTIGEST_E2E_ROOT")
            ?? throw new InvalidOperationException("Missing isolated output directory.");
        if (Directory.Exists(root)) throw new InvalidOperationException("Never reuse an E2E directory.");
        var database = new TestingDatabase();
        await database.InitializeAsync();
        await database.VerifyOwnershipAsync();
        await using var db = await database.CreateContextAsync();
        var password = "Qa5A!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        var users = new[] { "Admin", "Vendedor" }.Select(role => new Usuario
        {
            Nombre = "QA " + role, Apellido = "Sintetico", Email = role.ToLowerInvariant() + "@example.invalid",
            Role = role, PasswordHash = "", Comision = 0
        }).ToArray();
        foreach (var user in users) user.PasswordHash = new PasswordHasher<Usuario>().HashPassword(user, password);
        var clients = new[] { "Vehiculo", "Articulos", "Contado", "Sin pago", "Responsive" }
            .Select((name, index) => new Cliente
            {
                Nombre = "QA " + name, Dni = "QA5A-" + index, Telefono = "000", Direccion = "Testing",
                Email = "client-" + index + "@example.invalid", Usuario = users[0]
            }).ToArray();
        var brand = new MarcaProducto { Nombre = "QA Accesorios" };
        var sub = new SubCategoria { Nombre = "QA Seguridad", Categoria = new Categoria { Nombre = "QA Articulos" } };
        var products = new[]
        {
            new Producto { Nombre = "QA Casco", SKU = "QA-CASCO", PrecioVenta = 100, Stock = 10, MarcaProducto = brand, SubCategoria = sub },
            new Producto { Nombre = "QA Guantes", SKU = "QA-GUANTES", PrecioVenta = 50, Stock = 20, MarcaProducto = brand, SubCategoria = sub }
        };
        var vehicleBrand = new MarcaVehiculo { Nombre = "QA Motors" };
        var vehicles = Enumerable.Range(1, 6).Select(index => new Vehiculo
        {
            VIN = "QA-VIN-" + index, NumeroMotor = "QA-MOTOR-" + index, Patente = "QA-" + index,
            Anio = 2026, Color = "Azul", Estado = "Disponible", PrecioVenta = 1000,
            Modelo = new Modelo { Nombre = "QA Moto " + index, Cilindrada = 125, TipoMotor = "Testing", MarcaVehiculo = vehicleBrand }
        }).ToArray();
        var plans = new[]
        {
            new ConfiguracionFinanciacion { Activa = true, CantidadCuotas = 3, TasaInteres = 10, MontoMinimo = 700, MontoMaximo = 900 },
            new ConfiguracionFinanciacion { Activa = true, CantidadCuotas = 2, TasaInteres = 25, MontoMinimo = 700, MontoMaximo = 900 },
            new ConfiguracionFinanciacion { Activa = true, CantidadCuotas = 4, TasaInteres = 10, MontoMinimo = 1000, MontoMaximo = 2000 },
            new ConfiguracionFinanciacion { Activa = false, CantidadCuotas = 6, TasaInteres = 10, MontoMinimo = 0, MontoMaximo = 10000 }
        };
        db.AddRange(users); db.AddRange(clients); db.AddRange(products); db.AddRange(vehicles); db.AddRange(plans);
        await db.SaveChangesAsync();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Directory.CreateDirectory(root);
        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(Path.Combine(root, "appsettings.Testing.json"), JsonSerializer.Serialize(new
        {
            ConnectionStrings = new { DefaultConnection = db.Database.GetConnectionString() },
            FrontendUrl = "http://127.0.0.1:51851",
            Jwt = new { Key = Convert.ToHexString(RandomNumberGenerator.GetBytes(48)), Issuer = "OptiGest.Testing.5A", Audience = "OptiGest.Testing.5A" },
            TestingOwnership = database.Ownership,
            TestingDatabase = database.DatabaseName,
            Logging = new { LogLevel = new Dictionary<string, string> { ["Default"] = "Warning", ["Microsoft.Hosting.Lifetime"] = "Information" } }
        }, options));
        await File.WriteAllTextAsync(Path.Combine(root, "session.local.json"), JsonSerializer.Serialize(new
        {
            database.DatabaseName, database.Ownership,
            Api = "http://127.0.0.1:51850", Frontend = "http://127.0.0.1:51851",
            Email = users[0].Email, VendorEmail = users[1].Email, Password = password,
            Clients = clients.Select(c => new { c.Id, c.Nombre }), Products = products.Select(p => new { p.Id, p.Nombre, p.Stock, p.PrecioVenta }),
            Vehicles = vehicles.Select(v => new { v.Id, v.VIN }), Plans = plans.Select(p => new { p.Id, p.CantidadCuotas, p.TasaInteres, p.Activa })
        }, options));
        await File.WriteAllTextAsync(Path.Combine(root, "isolation.json"), JsonSerializer.Serialize(new
        {
            Instance = "OptiGest_Testing_Phase05", database.DatabaseName, database.Ownership,
            CreatedUtc = DateTime.UtcNow, NewDatabase = true, OwnershipVerified = true,
            AppliedMigrations = await db.Database.GetAppliedMigrationsAsync(), PendingMigrations = await db.Database.GetPendingMigrationsAsync(),
            Environment = "Testing", UserSecrets = false, InheritedApplicationEnvironment = false
        }, options));
    }

    [E2EUtilityFact("OPTIGEST_VERIFY_E2E")]
    public async Task Verify_current_run_persistence_read_only()
    {
        var root = Environment.GetEnvironmentVariable("OPTIGEST_E2E_ROOT")
            ?? throw new InvalidOperationException("The current run directory is required.");
        // Only this run's generated configuration. No default providers, environment or secrets.
        var config = new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.Testing.json").Build();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(config.GetConnectionString("DefaultConnection")).Options);
        await new DatabaseGuard(config).VerifyAsync(db);
        var sales = await db.Ventas.AsNoTracking().OrderBy(v => v.Id)
            .Select(v => new { v.Id, v.ClienteId, v.Fecha, v.Total, v.Estado,
                Detalles = v.DetallesVenta.Select(d => new { d.ProductoId, d.VehiculoId, d.Cantidad, d.PrecioUnitario }).ToArray(),
                Pagos = v.Pagos.Select(p => new { p.Monto, p.FormaPago }).ToArray() }).ToArrayAsync();
        var financings = await db.Financiaciones.AsNoTracking().OrderBy(f => f.Id)
            .Select(f => new { f.Id, f.VentaId, f.MontoFinanciado, f.MontoTotal, f.Estado }).ToArrayAsync();
        var quotas = await db.Cuotas.AsNoTracking().OrderBy(c => c.Id)
            .Select(c => new { c.Id, c.FinanciacionId, c.Numero, c.Monto, c.Estado }).ToArrayAsync();
        var payments = await db.PagosCuota.AsNoTracking().OrderBy(p => p.Id)
            .Select(p => new { p.Id, p.CuotaId, p.Monto }).ToArrayAsync();
        var products = await db.Productos.AsNoTracking().OrderBy(p => p.Id)
            .Select(p => new { p.Id, p.Nombre, p.Stock }).ToArrayAsync();
        var ledger = await db.CuentasCorrientes.AsNoTracking().OrderBy(m => m.Id)
            .Select(m => new { m.Id, m.ClienteId, m.Concepto, m.Debe, m.Haber, m.Saldo }).ToArrayAsync();
        var vehicles = await db.Vehiculos.AsNoTracking().OrderBy(v => v.Id)
            .Select(v => new { v.Id, v.Estado }).ToArrayAsync();
        Assert.Equal(6, sales.Length);
        Assert.Equal(4650m, sales.Sum(s => s.Total));
        Assert.Equal(2, sales[0].Detalles.Length);
        Assert.Equal(5, sales[0].Detalles.Single(d => d.ProductoId == 1).Cantidad);
        Assert.Equal(new[] { 5, 17 }, products.Select(p => p.Stock));
        Assert.Equal(4, vehicles.Count(v => v.Estado == "Vendido"));
        Assert.Equal("Pagada", sales.Single(s => s.Id == 2).Estado);
        Assert.Equal("Cancelada", financings.Single(f => f.Id == 1).Estado);
        Assert.Equal(880m, financings.Single(f => f.Id == 1).MontoTotal);
        Assert.Equal("Activa", financings.Single(f => f.Id == 2).Estado);
        Assert.Equal(3, quotas.Count(q => q.Estado == "Pagada"));
        Assert.Equal(3, payments.Length);
        Assert.Equal(880m, payments.Sum(p => p.Monto));
        Assert.Equal(6, ledger.Count(m => m.ClienteId == 1));
        Assert.Equal(0m, ledger.Where(m => m.ClienteId == 1).Sum(m => m.Debe - m.Haber));
        Assert.Equal(0m, ledger.Last(m => m.ClienteId == 1).Saldo);
        Assert.Equal(2600m, ledger.Sum(m => m.Debe - m.Haber));
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        // Retained evidence, never overwrite a previous verification.
        var output = Path.Combine(root, "database-verification-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".json");
        await using var file = new FileStream(output, FileMode.CreateNew);
        await JsonSerializer.SerializeAsync(file, new
        {
            Database = config["TestingDatabase"], OwnershipVerified = true, ReadOnly = true,
            VerifiedUtc = DateTime.UtcNow, Assertions = 18, Sales = sales, Financings = financings,
            Quotas = quotas, Payments = payments, Products = products, Ledger = ledger, Vehicles = vehicles
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}
