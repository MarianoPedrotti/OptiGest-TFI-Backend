using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;

namespace SistemaDeVentas.Repositories
{
    public class SubCategoriaRepository : ISubCategoriaRepository
    {
        private readonly AppDbContext _context;

        public SubCategoriaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SubCategoria>> ObtenerTodosAsync()
        {
            return await _context.SubCategorias
                .Include(s => s.Categoria)
                .ToListAsync();
        }

        public async Task<SubCategoria?> ObtenerPorIdAsync(int id)
        {
            return await _context.SubCategorias
                .Include(s => s.Categoria)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<SubCategoria?> ObtenerPorNombreAsync(string nombre)
        {
            return await _context.SubCategorias
                .FirstOrDefaultAsync(s => s.Nombre == nombre);
        }

        public async Task<SubCategoria> CrearAsync(SubCategoria subCategoria)
        {
            await _context.SubCategorias.AddAsync(subCategoria);
            await _context.SaveChangesAsync();

            return subCategoria;
        }

        public async Task ActualizarAsync(SubCategoria subCategoria)
        {
            _context.SubCategorias.Update(subCategoria);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(SubCategoria subCategoria)
        {
            _context.SubCategorias.Remove(subCategoria);
            await _context.SaveChangesAsync();
        }
        public async Task<List<SubCategoria>> ObtenerPorCategoriaAsync(int categoriaId)
        {
            return await _context.SubCategorias
                .Where(sc => sc.CategoriaId == categoriaId)
                .ToListAsync();
        }
    }
}