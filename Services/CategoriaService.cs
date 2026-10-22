using SistemaDeVentas.DTOs.Categorias;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly ICategoriaRepository _categoriaRepository;

        public CategoriaService(ICategoriaRepository categoriaRepository)
        {
            _categoriaRepository = categoriaRepository;
        }

        public async Task<List<CategoriaDto>> ObtenerTodosAsync()
        {
            var categorias = await _categoriaRepository.ObtenerTodosAsync();

            return categorias.Select(c => new CategoriaDto
            {
                Id = c.Id,
                Nombre = c.Nombre
            }).ToList();
        }

        public async Task<CategoriaDto?> ObtenerPorIdAsync(int id)
        {
            var categoria = await _categoriaRepository.ObtenerPorIdAsync(id);

            if (categoria == null)
                return null;

            return new CategoriaDto
            {
                Id = categoria.Id,
                Nombre = categoria.Nombre
            };
        }

        public async Task<CategoriaDto?> ObtenerPorNombreAsync(string nombre)
        {
            var categoria = await _categoriaRepository.ObtenerPorNombreAsync(nombre);

            if (categoria == null)
                return null;

            return new CategoriaDto
            {
                Id = categoria.Id,
                Nombre = categoria.Nombre
            };
        }
        public async Task<CategoriaDto> CrearAsync(CategoriaCreateDto dto)
        {
            var existente =
                await _categoriaRepository.ObtenerPorNombreAsync(dto.Nombre);

            if (existente != null)
                throw new Exception("Ya existe una categoría con ese nombre.");

            var categoria = new Categoria
            {
                Nombre = dto.Nombre
            };

            await _categoriaRepository.CrearAsync(categoria);

            return new CategoriaDto
            {
                Id = categoria.Id,
                Nombre = categoria.Nombre
            };
        }

        public async Task<bool> ActualizarAsync(CategoriaUpdateDto dto)
        {
            var categoria =
                await _categoriaRepository.ObtenerPorIdAsync(dto.Id);

            if (categoria == null)
                return false;

            var existente =
                await _categoriaRepository.ObtenerPorNombreAsync(dto.Nombre);

            if (existente != null && existente.Id != dto.Id)
                throw new Exception("Ya existe una categoría con ese nombre.");

            categoria.Nombre = dto.Nombre;

            await _categoriaRepository.ActualizarAsync(categoria);

            return true;
        }

        public async Task<bool> EliminarAsync(int id)
        {
            var categoria =
                await _categoriaRepository.ObtenerPorIdAsync(id);

            if (categoria == null)
                return false;

            await _categoriaRepository.EliminarAsync(categoria);

            return true;
        }
    }
}