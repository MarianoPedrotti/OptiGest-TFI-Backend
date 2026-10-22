using SistemaDeVentas.DTOs.SubCategorias;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Services
{
    public class SubCategoriaService : ISubCategoriaService
    {
        private readonly ISubCategoriaRepository _repository;

        public SubCategoriaService(ISubCategoriaRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<SubCategoriaDto>> ObtenerTodosAsync()
        {
            var lista = await _repository.ObtenerTodosAsync();

            return lista.Select(s => new SubCategoriaDto
            {
                Id = s.Id,
                Nombre = s.Nombre,
                CategoriaId = s.CategoriaId,
                NombreCategoria = s.Categoria.Nombre
            }).ToList();
        }

        public async Task<SubCategoriaDto?> ObtenerPorIdAsync(int id)
        {
            var sub = await _repository.ObtenerPorIdAsync(id);

            if (sub == null)
                return null;

            return new SubCategoriaDto
            {
                Id = sub.Id,
                Nombre = sub.Nombre,
                CategoriaId = sub.CategoriaId,
                NombreCategoria = sub.Categoria.Nombre
            };
        }

        public async Task<SubCategoriaDto?> ObtenerPorNombreAsync(string nombre)
        {
            var sub = await _repository.ObtenerPorNombreAsync(nombre);

            if (sub == null)
                return null;

            return new SubCategoriaDto
            {
                Id = sub.Id,
                Nombre = sub.Nombre,
                CategoriaId = sub.CategoriaId,
                NombreCategoria = sub.Categoria.Nombre
            };
        }

        public async Task<SubCategoriaDto> CrearAsync(SubCategoriaCreateDto dto)
        {
            var existente = await _repository.ObtenerPorNombreAsync(dto.Nombre);

            if (existente != null)
                throw new Exception("Ya existe una subcategoría con ese nombre.");

            var sub = new SubCategoria
            {
                Nombre = dto.Nombre,
                CategoriaId = dto.CategoriaId
            };

            await _repository.CrearAsync(sub);

            return new SubCategoriaDto
            {
                Id = sub.Id,
                Nombre = sub.Nombre,
                CategoriaId = sub.CategoriaId
            };
        }

        public async Task<bool> ActualizarAsync(SubCategoriaUpdateDto dto)
        {
            var sub = await _repository.ObtenerPorIdAsync(dto.Id);

            if (sub == null)
                return false;

            sub.Nombre = dto.Nombre;
            sub.CategoriaId = dto.CategoriaId;

            await _repository.ActualizarAsync(sub);

            return true;
        }

        public async Task<bool> EliminarAsync(int id)
        {
            var sub = await _repository.ObtenerPorIdAsync(id);

            if (sub == null)
                return false;

            await _repository.EliminarAsync(sub);

            return true;
        }
        public async Task<List<SubCategoriaDto>> ObtenerPorCategoriaAsync(int categoriaId)
        {
            var subCategorias =
                await _repository.ObtenerPorCategoriaAsync(categoriaId);

            return subCategorias.Select(sc => new SubCategoriaDto
            {
                Id = sc.Id,
                Nombre = sc.Nombre,
                CategoriaId = sc.CategoriaId
            }).ToList();
        }
    }
}