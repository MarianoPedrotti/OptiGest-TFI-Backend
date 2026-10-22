using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;

namespace SistemaDeVentas.Repositories
{
    public class CategoriaRepository : ICategoriaRepository
    {
        private readonly AppDbContext _context;

        public CategoriaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Categoria>> ObtenerTodosAsync()
        {
            return await _context.Categorias
                .OrderBy(c => c.Nombre)
                .ToListAsync();
        }

        public async Task<Categoria?> ObtenerPorIdAsync(int id)
        {
            return await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Categoria?> ObtenerPorNombreAsync(string nombre)
        {
            return await _context.Categorias
                .FirstOrDefaultAsync(c => c.Nombre == nombre);
        }

        public async Task<Categoria> CrearAsync(Categoria categoria)
        {
            await _context.Categorias.AddAsync(categoria);

            await _context.SaveChangesAsync();

            return categoria;
        }

        public async Task ActualizarAsync(Categoria categoria)
        {
            _context.Categorias.Update(categoria);

            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(Categoria categoria)
        {
            _context.Categorias.Remove(categoria);

            await _context.SaveChangesAsync();
        }
    }
}