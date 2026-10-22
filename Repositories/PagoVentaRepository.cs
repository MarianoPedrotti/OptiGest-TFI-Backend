using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;

namespace SistemaDeVentas.Repositories
{
    public class PagoVentaRepository : IPagoVentaRepository
    {
        private readonly AppDbContext _context;

        public PagoVentaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PagoVenta>> ObtenerTodosAsync()
        {
            return await _context.PagosVenta
                .Include(p => p.Venta)
                .ToListAsync();
        }

        public async Task<PagoVenta?> ObtenerPorIdAsync(int id)
        {
            return await _context.PagosVenta
                .Include(p => p.Venta)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PagoVenta> CrearAsync(PagoVenta pago)
        {
            await _context.PagosVenta.AddAsync(pago);
            await _context.SaveChangesAsync();

            return pago;
        }
    }
}