using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;

namespace SistemaDeVentas.Repositories
{
    public class ConfiguracionFinanciacionRepository : IConfiguracionFinanciacionRepository
    {
        private readonly AppDbContext _context;

        public ConfiguracionFinanciacionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ConfiguracionFinanciacion>> ObtenerTodosAsync()
        {
            return await _context.ConfiguracionesFinanciacion
                .OrderBy(c => c.CantidadCuotas)
                .ToListAsync();
        }

        public async Task<List<ConfiguracionFinanciacion>> ObtenerActivasAsync()
        {
            return await _context.ConfiguracionesFinanciacion
                .Where(c => c.Activa)
                .OrderBy(c => c.CantidadCuotas)
                .ToListAsync();
        }

        public async Task<ConfiguracionFinanciacion?> ObtenerPorIdAsync(int id)
        {
            return await _context.ConfiguracionesFinanciacion
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<ConfiguracionFinanciacion> CrearAsync(ConfiguracionFinanciacion configuracion)
        {
            await _context.ConfiguracionesFinanciacion.AddAsync(configuracion);

            await _context.SaveChangesAsync();

            return configuracion;
        }

        public async Task ActualizarAsync(ConfiguracionFinanciacion configuracion)
        {
            _context.ConfiguracionesFinanciacion.Update(configuracion);

            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(ConfiguracionFinanciacion configuracion)
        {
            _context.ConfiguracionesFinanciacion.Remove(configuracion);

            await _context.SaveChangesAsync();
        }
        public async Task<bool> TieneFinanciacionesAsync(int id)
        {
            return await _context.Financiaciones
                .AnyAsync(
                    f => f.ConfiguracionFinanciacionId == id
                );
        }
    }
}