using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;

public class CuotaRepository : ICuotaRepository
{
    private readonly AppDbContext _context;

    public CuotaRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<int?> ObtenerClienteIdAsync(int id) => _context.Cuotas.Where(c => c.Id == id)
        .Select(c => (int?)c.Financiacion.Venta.ClienteId).SingleOrDefaultAsync();

    public async Task ActualizarEstadoAsync(int id, string estado)
    {
        var cuota = await _context.Cuotas.FindAsync(id) ?? throw new InvalidOperationException("Cuota inexistente.");
        cuota.Estado = estado;
        _context.Entry(cuota).Property(c => c.Estado).IsModified = true;
        await _context.SaveChangesAsync();
    }

    public async Task<List<Cuota>> ObtenerTodosAsync()
    {
        return await _context.Cuotas
            .Include(c => c.Pagos)
             .Include(c => c.Financiacion)
            .ThenInclude(f => f.Venta)
                .ThenInclude(v => v.Cliente)
            .ToListAsync();
    }

    public async Task<Cuota?> ObtenerPorIdAsync(int id)
    {
        return await _context.Cuotas
            .Include(c => c.Pagos)
            .Include(c => c.Financiacion)
            .ThenInclude(f => f.Venta)
            .ThenInclude(v => v.Cliente)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cuota> CrearAsync(Cuota cuota)
    {
        await _context.Cuotas.AddAsync(cuota);
        await _context.SaveChangesAsync();

        return cuota;
    }

    public async Task ActualizarAsync(Cuota cuota)
    {
        _context.Cuotas.Update(cuota);
        await _context.SaveChangesAsync();
    }
    public async Task<List<Cuota>> ObtenerPorFinanciacionAsync(int financiacionId)
    {
        return await _context.Cuotas
            .Where(c => c.FinanciacionId == financiacionId)
            .Include(c => c.Financiacion)
                .ThenInclude(f => f.Venta)
                    .ThenInclude(v => v.Cliente)
            .ToListAsync();
    }
    public async Task<int> ContarVencidasAsync()
    {
        return await _context.Cuotas
            .CountAsync(c =>
                c.Estado == "Pendiente"
                && c.FechaVencimiento.Date < DateTime.Today);
    }
    public async Task<List<Cuota>> ObtenerProximasAsync(int cantidad)
    {
        return await _context.Cuotas
            .Include(c => c.Financiacion)
                .ThenInclude(f => f.Venta)
                    .ThenInclude(v => v.Cliente)
            .Where(c => c.Estado == "Pendiente")
            .OrderBy(c => c.FechaVencimiento)
            .Take(cantidad)
            .ToListAsync();
    }
}
