using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;

namespace SistemaDeVentas.Repositories
{
    public class MarcaVehiculoRepository : IMarcaVehiculoRepository
    {
        private readonly AppDbContext _context;

        public MarcaVehiculoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<MarcaVehiculo>> ObtenerTodosAsync()
        {
            return await _context.MarcasVehiculo
                .OrderBy(x => x.Nombre)
                .ToListAsync();
        }

        public async Task<MarcaVehiculo?> ObtenerPorIdAsync(int id)
        {
            return await _context.MarcasVehiculo
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<MarcaVehiculo?> ObtenerPorNombreAsync(string nombre)
        {
            return await _context.MarcasVehiculo
                .FirstOrDefaultAsync(x => x.Nombre == nombre);
        }

        public async Task<MarcaVehiculo> CrearAsync(MarcaVehiculo marca)
        {
            await _context.MarcasVehiculo.AddAsync(marca);

            await _context.SaveChangesAsync();

            return marca;
        }

        public async Task ActualizarAsync(MarcaVehiculo marca)
        {
            _context.MarcasVehiculo.Update(marca);

            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(MarcaVehiculo marca)
        {
            _context.MarcasVehiculo.Remove(marca);

            await _context.SaveChangesAsync();
        }
    }
}