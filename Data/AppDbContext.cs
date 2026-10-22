using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Entities;

namespace SistemaDeVentas.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }

        public DbSet<Cliente> Clientes { get; set; }

        public DbSet<Producto> Productos { get; set; }

        public DbSet<Vehiculo> Vehiculos { get; set; }
        public DbSet<Venta> Ventas { get; set; }

        public DbSet<DetalleVenta> DetallesVenta { get; set; }

        public DbSet<PagoVenta> PagosVenta { get; set; }

        public DbSet<ConfiguracionFinanciacion> ConfiguracionesFinanciacion { get; set; }

        public DbSet<Financiacion> Financiaciones { get; set; }

        public DbSet<Cuota> Cuotas { get; set; }

        public DbSet<PagoCuota> PagosCuota { get; set; }

        public DbSet<CuentaCorriente> CuentasCorrientes { get; set; }
        public DbSet<MarcaProducto> MarcasProducto { get; set; }

        public DbSet<MarcaVehiculo> MarcasVehiculo { get; set; }

        public DbSet<Categoria> Categorias { get; set; }

        public DbSet<SubCategoria> SubCategorias { get; set; }

        public DbSet<Modelo> Modelos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Índices únicos

            modelBuilder.Entity<Usuario>()
                .HasIndex(x => x.Email)
                .IsUnique();

            modelBuilder.Entity<Cliente>()
                .HasIndex(x => x.Dni)
                .IsUnique();

            modelBuilder.Entity<Producto>()
                .HasIndex(x => x.SKU)
                .IsUnique();

            modelBuilder.Entity<Vehiculo>()
                .HasIndex(x => x.VIN)
                .IsUnique();
            modelBuilder.Entity<Vehiculo>()
                .HasIndex(x => x.NumeroMotor)
                .IsUnique();
            modelBuilder.Entity<MarcaProducto>()
                .HasIndex(m => m.Nombre)
                .IsUnique();
            modelBuilder.Entity<MarcaVehiculo>()
                .HasIndex(m => m.Nombre)
                .IsUnique();
            modelBuilder.Entity<Categoria>()
                .HasIndex(c => c.Nombre)
                .IsUnique();
            modelBuilder.Entity<SubCategoria>()
                .HasIndex(s => new
                    {
                        s.CategoriaId,
                        s.Nombre
                    })
                .IsUnique();
            modelBuilder.Entity<Modelo>()
            .HasIndex(m => new
                {
                    m.MarcaVehiculoId,
                    m.Nombre
                })
            .IsUnique();

            // Relaciones

            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.Usuario)
                .WithMany(u => u.Clientes)
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Cliente)
                .WithMany(c => c.Ventas)
                .HasForeignKey(v => v.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Usuario)
                .WithMany(u => u.Ventas)
                .HasForeignKey(v => v.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SubCategoria>()
                .HasOne(s => s.Categoria)
                .WithMany(c => c.SubCategorias)
                .HasForeignKey(s => s.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.MarcaProducto)
                .WithMany(m => m.Productos)
                .HasForeignKey(p => p.MarcaProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.SubCategoria)
                .WithMany(s => s.Productos)
                .HasForeignKey(p => p.SubCategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Modelo>()
                .HasOne(m => m.MarcaVehiculo)
                .WithMany(mv => mv.Modelos)
                .HasForeignKey(m => m.MarcaVehiculoId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Vehiculo>()
                .HasOne(v => v.Modelo)
                .WithMany(m => m.Vehiculos)
                .HasForeignKey(v => v.ModeloId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DetalleVenta>()
                .HasOne(d => d.Producto)
                .WithMany(p => p.DetallesVenta)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DetalleVenta>()
                .HasOne(d => d.Vehiculo)
                .WithMany(v => v.DetallesVenta)
                .HasForeignKey(d => d.VehiculoId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DetalleVenta>()
    .HasOne(d => d.Venta)
    .WithMany(v => v.DetallesVenta)
    .HasForeignKey(d => d.VentaId)
    .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PagoVenta>()
    .HasOne(p => p.Venta)
    .WithMany(v => v.Pagos)
    .HasForeignKey(p => p.VentaId)
    .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Financiacion>()
    .HasOne(f => f.Venta)
    .WithOne(v => v.Financiacion)
    .HasForeignKey<Financiacion>(f => f.VentaId)
    .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Financiacion>()
    .HasOne(f => f.ConfiguracionFinanciacion)
    .WithMany(c => c.Financiaciones)
    .HasForeignKey(f => f.ConfiguracionFinanciacionId)
    .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Cuota>()
    .HasOne(c => c.Financiacion)
    .WithMany(f => f.Cuotas)
    .HasForeignKey(c => c.FinanciacionId)
    .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PagoCuota>()
    .HasOne(p => p.Cuota)
    .WithMany(c => c.Pagos)
    .HasForeignKey(p => p.CuotaId)
    .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CuentaCorriente>()
    .HasOne(c => c.Cliente)
    .WithMany(c => c.MovimientosCuentaCorriente)
    .HasForeignKey(c => c.ClienteId)
    .OnDelete(DeleteBehavior.Restrict);
            // ==========================
            // Precisión decimal
            // ==========================

            // Producto
            modelBuilder.Entity<Producto>()
                .Property(p => p.PrecioCompra)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Producto>()
                .Property(p => p.PrecioVenta)
                .HasPrecision(18, 2);


            // Vehículo
            modelBuilder.Entity<Vehiculo>()
                .Property(v => v.PrecioCompra)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Vehiculo>()
                .Property(v => v.PrecioVenta)
                .HasPrecision(18, 2);


            // Venta
            modelBuilder.Entity<Venta>()
                .Property(v => v.Subtotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Venta>()
                .Property(v => v.Total)
                .HasPrecision(18, 2);


            // Detalle venta
            modelBuilder.Entity<DetalleVenta>()
                .Property(d => d.PrecioUnitario)
                .HasPrecision(18, 2);


            // Pago de venta
            modelBuilder.Entity<PagoVenta>()
                .Property(p => p.Monto)
                .HasPrecision(18, 2);


            // Configuración financiación
            modelBuilder.Entity<ConfiguracionFinanciacion>()
                .Property(c => c.MontoMinimo)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ConfiguracionFinanciacion>()
                .Property(c => c.MontoMaximo)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ConfiguracionFinanciacion>()
                .Property(c => c.TasaInteres)
                .HasPrecision(18, 4);


            // Financiación
            modelBuilder.Entity<Financiacion>()
                .Property(f => f.MontoFinanciado)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Financiacion>()
                .Property(f => f.MontoTotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Financiacion>()
                .Property(f => f.CoeficienteAplicado)
                .HasPrecision(18, 4);


            // Cuota
            modelBuilder.Entity<Cuota>()
                .Property(c => c.Monto)
                .HasPrecision(18, 2);


            // Pago cuota
            modelBuilder.Entity<PagoCuota>()
                .Property(p => p.Monto)
                .HasPrecision(18, 2);


            // Cuenta corriente
            modelBuilder.Entity<CuentaCorriente>()
                .Property(c => c.Debe)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CuentaCorriente>()
                .Property(c => c.Haber)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CuentaCorriente>()
                .Property(c => c.Saldo)
                .HasPrecision(18, 2);


            // Usuario
            modelBuilder.Entity<Usuario>()
                .Property(u => u.Comision)
                .HasPrecision(18, 4);
        }




    }

}

