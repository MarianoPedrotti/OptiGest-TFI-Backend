using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;

namespace SistemaDeVentas.Repositories
{
    public class ModeloRepository : IModeloRepository
    {
        private readonly AppDbContext _context;

        public ModeloRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Modelo>> ObtenerTodosAsync()
        {
            return await _context.Modelos
                .Include(x => x.MarcaVehiculo)
                .ToListAsync();
        }

        public async Task<Modelo?> ObtenerPorIdAsync(int id)
        {
            return await _context.Modelos
                .Include(x => x.MarcaVehiculo)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Modelo?> ObtenerPorNombreAsync(string nombre)
        {
            return await _context.Modelos
                .FirstOrDefaultAsync(x => x.Nombre == nombre);
        }

        public async Task<Modelo> CrearAsync(Modelo modelo)
        {
            await _context.Modelos.AddAsync(modelo);

            await _context.SaveChangesAsync();

            return modelo;
        }

        public async Task ActualizarAsync(Modelo modelo)
        {
            _context.Modelos.Update(modelo);

            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(Modelo modelo)
        {
            _context.Modelos.Remove(modelo);

            await _context.SaveChangesAsync();
        }
        public async Task<List<Modelo>> ObtenerPorMarcaAsync(int marcaVehiculoId)
        {
            return await _context.Modelos
                .Where(x => x.MarcaVehiculoId == marcaVehiculoId)
                .OrderBy(x => x.Nombre)
                .ToListAsync();
        }
    }
}