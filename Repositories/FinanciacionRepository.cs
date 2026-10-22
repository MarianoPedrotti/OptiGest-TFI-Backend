using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.DTOs.Financiaciones;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;

namespace SistemaDeVentas.Repositories
{
    public class FinanciacionRepository : IFinanciacionRepository
    {
        private readonly AppDbContext _context;

        public FinanciacionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task ActualizarEstadoAsync(int id, string estado)
        {
            var financiacion = await _context.Financiaciones.FindAsync(id) ?? throw new InvalidOperationException("Financiación inexistente.");
            financiacion.Estado = estado;
            _context.Entry(financiacion).Property(f => f.Estado).IsModified = true;
            await _context.SaveChangesAsync();
        }

        public async Task<Financiacion?> ObtenerPorIdAsync(int id)
        {
            return await _context.Financiaciones
                .Include(f => f.ConfiguracionFinanciacion)

                .Include(f => f.Cuotas)

                .Include(f => f.Venta)
                    .ThenInclude(v => v.Cliente)

                .Include(f => f.Venta)
                    .ThenInclude(v => v.DetallesVenta)
                        .ThenInclude(d => d.Vehiculo)
                            .ThenInclude(v => v.Modelo)
                                .ThenInclude(m => m.MarcaVehiculo)

                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<List<Financiacion>> ObtenerTodosAsync()
        {
            return await _context.Financiaciones
                .Include(f => f.ConfiguracionFinanciacion)

                .Include(f => f.Cuotas)

                .Include(f => f.Venta)
                    .ThenInclude(v => v.Cliente)

                .Include(f => f.Venta)
                    .ThenInclude(v => v.DetallesVenta)
                        .ThenInclude(d => d.Vehiculo)
                            .ThenInclude(v => v.Modelo)
                                .ThenInclude(m => m.MarcaVehiculo)

                .OrderByDescending(f => f.FechaCreacion)
                .ThenByDescending(f => f.Id)
                .ToListAsync();
        }

        public async Task<Financiacion> CrearAsync(Financiacion financiacion)
        {
            await _context.Financiaciones.AddAsync(financiacion);
            await _context.SaveChangesAsync();

            return financiacion;
        }

        public async Task ActualizarAsync(Financiacion financiacion)
        {
            _context.Financiaciones.Update(financiacion);
            await _context.SaveChangesAsync();
        }
        public async Task<int> ContarActivasAsync()
        {
            return await _context.Financiaciones
                .CountAsync(f => f.Estado == "Activa");
        }
        public async Task<FinanciacionResumenDto> ObtenerResumenAsync()
        {
            var financiaciones =
                await _context.Financiaciones
                    .AsNoTracking()
                    .Select(f => new
                    {
                        f.Estado,

                        f.MontoFinanciado,

                        f.MontoTotal,

                        MontoPagado =
                            f.Cuotas
                                .Where(c => c.Estado == "Pagada")
                                .Sum(c => (decimal?)c.Monto) ?? 0
                    })
                    .ToListAsync();


            return new FinanciacionResumenDto
            {
                Activas =
                    financiaciones.Count(
                        f => f.Estado == "Activa"
                    ),

                Canceladas =
                    financiaciones.Count(
                        f => f.Estado == "Cancelada"
                    ),

                TotalFinanciado =
                    financiaciones.Sum(
                        f => f.MontoFinanciado
                    ),

                SaldoPendiente =
                    financiaciones
                        .Where(f => f.Estado == "Activa")
                        .Sum(f =>
                            f.MontoTotal - f.MontoPagado
                        )
            };
        }
    }
}
