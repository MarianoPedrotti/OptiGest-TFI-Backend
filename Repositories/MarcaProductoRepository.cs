using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;

namespace SistemaDeVentas.Repositories
{
    public class MarcaProductoRepository : IMarcaProductoRepository
    {
        private readonly AppDbContext _context;

        public MarcaProductoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<MarcaProducto>> ObtenerTodosAsync()
        {
            return await _context.MarcasProducto
                .OrderBy(x => x.Nombre)
                .ToListAsync();
        }

        public async Task<MarcaProducto?> ObtenerPorIdAsync(int id)
        {
            return await _context.MarcasProducto
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<MarcaProducto?> ObtenerPorNombreAsync(string nombre)
        {
            return await _context.MarcasProducto
                .FirstOrDefaultAsync(x => x.Nombre == nombre);
        }

        public async Task<MarcaProducto> CrearAsync(MarcaProducto marcaProducto)
        {
            await _context.MarcasProducto.AddAsync(marcaProducto);

            await _context.SaveChangesAsync();

            return marcaProducto;
        }

        public async Task ActualizarAsync(MarcaProducto marcaProducto)
        {
            _context.MarcasProducto.Update(marcaProducto);

            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(MarcaProducto marcaProducto)
        {
            _context.MarcasProducto.Remove(marcaProducto);

            await _context.SaveChangesAsync();
        }
    }
}