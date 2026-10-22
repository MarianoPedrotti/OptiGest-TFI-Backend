using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;

namespace SistemaDeVentas.Repositories
{
    public class PagoCuotaRepository : IPagoCuotaRepository
    {
        private readonly AppDbContext _context;

        public PagoCuotaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PagoCuota>> ObtenerTodosAsync()
        {
            return await _context.PagosCuota
                .Include(p => p.Cuota)
                .ThenInclude(c => c.Financiacion)
                .ThenInclude(f => f.Venta)
                    .ThenInclude(v => v.Cliente)
                .ToListAsync();
        }

        public async Task<PagoCuota?> ObtenerPorIdAsync(int id)
        {
            return await _context.PagosCuota
                .Include(p => p.Cuota)
                .ThenInclude(c => c.Financiacion)
                .ThenInclude(f => f.Venta)
                    .ThenInclude(v => v.Cliente)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PagoCuota> CrearAsync(PagoCuota pagoCuota)
        {
            await _context.PagosCuota.AddAsync(pagoCuota);
            await _context.SaveChangesAsync();

            return pagoCuota;
        }

        public async Task ActualizarAsync(PagoCuota pagoCuota)
        {
            _context.PagosCuota.Update(pagoCuota);
            await _context.SaveChangesAsync();
        }
    }
}