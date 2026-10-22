using SistemaDeVentas.DTOs.MarcaProducto;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Services
{
    public class MarcaProductoService : IMarcaProductoService
    {
        private readonly IMarcaProductoRepository _marcaProductoRepository;

        public MarcaProductoService(IMarcaProductoRepository marcaProductoRepository)
        {
            _marcaProductoRepository = marcaProductoRepository;
        }

        public async Task<List<MarcaProductoDto>> ObtenerTodosAsync()
        {
            var marcas = await _marcaProductoRepository.ObtenerTodosAsync();

            return marcas.Select(m => new MarcaProductoDto
            {
                Id = m.Id,
                Nombre = m.Nombre
            }).ToList();
        }

        public async Task<MarcaProductoDto?> ObtenerPorIdAsync(int id)
        {
            var marca = await _marcaProductoRepository.ObtenerPorIdAsync(id);

            if (marca == null)
                return null;

            return new MarcaProductoDto
            {
                Id = marca.Id,
                Nombre = marca.Nombre
            };
        }

        public async Task<MarcaProductoDto> CrearAsync(MarcaProductoCreateDto dto)
        {
            var existente =
                await _marcaProductoRepository.ObtenerPorNombreAsync(dto.Nombre);

            if (existente != null)
                throw new Exception("Ya existe una marca con ese nombre.");

            var marca = new MarcaProducto
            {
                Nombre = dto.Nombre
            };

            await _marcaProductoRepository.CrearAsync(marca);

            return new MarcaProductoDto
            {
                Id = marca.Id,
                Nombre = marca.Nombre
            };
        }

        public async Task<bool> ActualizarAsync(MarcaProductoUpdateDto dto)
        {
            var marca =
                await _marcaProductoRepository.ObtenerPorIdAsync(dto.Id);

            if (marca == null)
                return false;

            var existente =
                await _marcaProductoRepository.ObtenerPorNombreAsync(dto.Nombre);

            if (existente != null && existente.Id != dto.Id)
                throw new Exception("Ya existe una marca con ese nombre.");

            marca.Nombre = dto.Nombre;

            await _marcaProductoRepository.ActualizarAsync(marca);

            return true;
        }

        public async Task<bool> EliminarAsync(int id)
        {
            var marca =
                await _marcaProductoRepository.ObtenerPorIdAsync(id);

            if (marca == null)
                return false;

            await _marcaProductoRepository.EliminarAsync(marca);

            return true;
        }
    }
}