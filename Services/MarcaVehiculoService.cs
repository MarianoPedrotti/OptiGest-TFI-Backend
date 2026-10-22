using SistemaDeVentas.DTOs.MarcaVehiculo;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Services
{
    public class MarcaVehiculoService : IMarcaVehiculoService
    {
        private readonly IMarcaVehiculoRepository _marcaVehiculoRepository;

        public MarcaVehiculoService(IMarcaVehiculoRepository marcaVehiculoRepository)
        {
            _marcaVehiculoRepository = marcaVehiculoRepository;
        }

        public async Task<List<MarcaVehiculoDto>> ObtenerTodosAsync()
        {
            var marcas = await _marcaVehiculoRepository.ObtenerTodosAsync();

            return marcas.Select(m => new MarcaVehiculoDto
            {
                Id = m.Id,
                Nombre = m.Nombre
            }).ToList();
        }

        public async Task<MarcaVehiculoDto?> ObtenerPorIdAsync(int id)
        {
            var marca = await _marcaVehiculoRepository.ObtenerPorIdAsync(id);

            if (marca == null)
                return null;

            return new MarcaVehiculoDto
            {
                Id = marca.Id,
                Nombre = marca.Nombre
            };
        }

        public async Task<MarcaVehiculoDto> CrearAsync(MarcaVehiculoCreateDto dto)
        {
            var existente =
                await _marcaVehiculoRepository.ObtenerPorNombreAsync(dto.Nombre);

            if (existente != null)
                throw new Exception("Ya existe una marca con ese nombre.");

            var marca = new MarcaVehiculo
            {
                Nombre = dto.Nombre
            };

            await _marcaVehiculoRepository.CrearAsync(marca);

            return new MarcaVehiculoDto
            {
                Id = marca.Id,
                Nombre = marca.Nombre
            };
        }

        public async Task<bool> ActualizarAsync(MarcaVehiculoUpdateDto dto)
        {
            var marca =
                await _marcaVehiculoRepository.ObtenerPorIdAsync(dto.Id);

            if (marca == null)
                return false;

            var existente =
                await _marcaVehiculoRepository.ObtenerPorNombreAsync(dto.Nombre);

            if (existente != null && existente.Id != dto.Id)
                throw new Exception("Ya existe una marca con ese nombre.");

            marca.Nombre = dto.Nombre;

            await _marcaVehiculoRepository.ActualizarAsync(marca);

            return true;
        }

        public async Task<bool> EliminarAsync(int id)
        {
            var marca =
                await _marcaVehiculoRepository.ObtenerPorIdAsync(id);

            if (marca == null)
                return false;

            await _marcaVehiculoRepository.EliminarAsync(marca);

            return true;
        }
    }
}