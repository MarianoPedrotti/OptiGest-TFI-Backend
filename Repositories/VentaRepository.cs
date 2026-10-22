using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.DTOs.Ventas;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;

namespace SistemaDeVentas.Repositories
{
    public class VentaRepository: IVentaRepository
    {
        private readonly AppDbContext _context;

        public VentaRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<int?> ObtenerClienteIdAsync(int id) => _context.Ventas
            .Where(v => v.Id == id).Select(v => (int?)v.ClienteId).SingleOrDefaultAsync();

        public async Task ActualizarEstadoAsync(int id, string estado)
        {
            var venta = await _context.Ventas.FindAsync(id) ?? throw new InvalidOperationException("Venta inexistente.");
            venta.Estado = estado;
            _context.Entry(venta).Property(v => v.Estado).IsModified = true;
            await _context.SaveChangesAsync();
        }

        public async Task<List<Venta>> ObtenerTodosAsync()
        {
            return await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .Include(v => v.DetallesVenta)
                .Include(v => v.Financiacion)
                .Include(v => v.Pagos)
                .OrderByDescending(v => v.Fecha)
                .ThenByDescending(v => v.Id)
                .ToListAsync();
        }

        public async Task<Venta?> ObtenerPorIdAsync(int id)
        {
            return await _context.Ventas

                .AsNoTracking()
                .AsSplitQuery()

                .Include(v => v.Cliente)

                .Include(v => v.Usuario)

                .Include(v => v.DetallesVenta)
                    .ThenInclude(d => d.Producto)

                .Include(v => v.DetallesVenta)
                    .ThenInclude(d => d.Vehiculo)
                        .ThenInclude(v => v.Modelo)
                            .ThenInclude(m => m.MarcaVehiculo)

                .Include(v => v.Pagos)

                .Include(v => v.Financiacion)
                    .ThenInclude(f => f.Cuotas)

                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<Venta> CrearAsync(Venta venta)
        {
            await _context.Ventas.AddAsync(venta);
            await _context.SaveChangesAsync();

            return venta;
        }

        public async Task ActualizarAsync(Venta venta)
        {
            _context.Ventas.Update(venta);
            await _context.SaveChangesAsync();
        }
        public async Task<decimal> ObtenerVentasDelDiaAsync()
        {
            var hoy = DateTime.Today;

            return await _context.Ventas
                .Where(v => v.Fecha.Date == hoy)
                .SumAsync(v => (decimal?)v.Total) ?? 0;
        }
        public async Task<List<Venta>> ObtenerUltimasAsync(int cantidad)
        {
            return await _context.Ventas
                .Include(v => v.Cliente)
                .OrderByDescending(v => v.Fecha)
                .Take(cantidad)
                .ToListAsync();
        }
        public async Task<List<Venta>> ObtenerVentasUltimos7DiasAsync()
        {
            var desde = DateTime.Today.AddDays(-6);
            var hasta = DateTime.Today.AddDays(1);

            return await _context.Ventas
                .Where(v => v.Fecha >= desde && v.Fecha < hasta)
                .OrderBy(v => v.Fecha)
                .ToListAsync();
        }
        public async Task<VentaResumenDto> ObtenerResumenAsync()
        {
            var inicioMes =
                new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1
                );

            var inicioMesSiguiente =
                inicioMes.AddMonths(1);


            var ventasDelMes =
                await _context.Ventas
                    .CountAsync(v =>
                        v.Fecha >= inicioMes &&
                        v.Fecha < inicioMesSiguiente
                    );


            var montoVendidoMes =
                await _context.Ventas
                    .Where(v =>
                        v.Fecha >= inicioMes &&
                        v.Fecha < inicioMesSiguiente
                    )
                    .SumAsync(v => (decimal?)v.Total) ?? 0;


            var pendientes =
                await _context.Ventas
                    .CountAsync(v =>
                        v.Estado == "Pendiente" ||
                        v.Estado == "PagoParcial" ||
                        v.Estado == "Parcial" // Alias histórico; las nuevas escrituras usan PagoParcial.
                    );


            var pagadas =
                await _context.Ventas
                    .CountAsync(v =>
                        v.Estado == "Pagada"
                    );


            return new VentaResumenDto
            {
                VentasDelMes = ventasDelMes,

                MontoVendidoMes = montoVendidoMes,

                Pendientes = pendientes,

                Pagadas = pagadas
            };
        }
    }
}

