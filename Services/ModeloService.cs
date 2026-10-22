using SistemaDeVentas.DTOs.Modelos;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Services
{
    public class ModeloService : IModeloService
    {
        private readonly IModeloRepository _modeloRepository;

        public ModeloService(IModeloRepository modeloRepository)
        {
            _modeloRepository = modeloRepository;
        }

        public async Task<List<ModeloDto>> ObtenerTodosAsync()
        {
            var modelos = await _modeloRepository.ObtenerTodosAsync();

            return modelos.Select(m => new ModeloDto
            {
                Id = m.Id,
                MarcaVehiculoId = m.MarcaVehiculoId,
                Nombre = m.Nombre,
                Cilindrada = m.Cilindrada,
                TipoMotor = m.TipoMotor,
                nombreMarca = m.MarcaVehiculo.Nombre    
            }).ToList();
        }

        public async Task<ModeloDto?> ObtenerPorIdAsync(int id)
        {
            var modelo = await _modeloRepository.ObtenerPorIdAsync(id);

            if (modelo == null)
                return null;

            return new ModeloDto
            {
                Id = modelo.Id,
                MarcaVehiculoId = modelo.MarcaVehiculoId,
                Nombre = modelo.Nombre,
                Cilindrada = modelo.Cilindrada,
                TipoMotor = modelo.TipoMotor
            };
        }

        public async Task<ModeloDto?> ObtenerPorNombreAsync(string nombre)
        {
            var modelo = await _modeloRepository.ObtenerPorNombreAsync(nombre);

            if (modelo == null)
                return null;

            return new ModeloDto
            {
                Id = modelo.Id,
                MarcaVehiculoId = modelo.MarcaVehiculoId,
                Nombre = modelo.Nombre,
                Cilindrada = modelo.Cilindrada,
                TipoMotor = modelo.TipoMotor
            };
        }

        public async Task<List<ModeloDto>> ObtenerPorMarcaAsync(int marcaVehiculoId)
        {
            var modelos = await _modeloRepository.ObtenerPorMarcaAsync(marcaVehiculoId);

            return modelos.Select(m => new ModeloDto
            {
                Id = m.Id,
                MarcaVehiculoId = m.MarcaVehiculoId,
                Nombre = m.Nombre,
                Cilindrada = m.Cilindrada,
                TipoMotor = m.TipoMotor
            }).ToList();
        }

        public async Task<ModeloDto> CrearAsync(ModeloCreateDto dto)
        {
            var existente = await _modeloRepository.ObtenerPorNombreAsync(dto.Nombre);

            if (existente != null)
                throw new Exception("Ya existe un modelo con ese nombre.");

            var modelo = new Modelo
            {
                MarcaVehiculoId = dto.MarcaVehiculoId,
                Nombre = dto.Nombre,
                Cilindrada = dto.Cilindrada,
                TipoMotor = dto.TipoMotor
            };

            await _modeloRepository.CrearAsync(modelo);

            return new ModeloDto
            {
                Id = modelo.Id,
                MarcaVehiculoId = modelo.MarcaVehiculoId,
                Nombre = modelo.Nombre,
                Cilindrada = modelo.Cilindrada,
                TipoMotor = modelo.TipoMotor
            };
        }

        public async Task<bool> ActualizarAsync(ModeloUpdateDto dto)
        {
            var modelo = await _modeloRepository.ObtenerPorIdAsync(dto.Id);

            if (modelo == null)
                return false;

            var existente = await _modeloRepository.ObtenerPorNombreAsync(dto.Nombre);

            if (existente != null && existente.Id != dto.Id)
                throw new Exception("Ya existe un modelo con ese nombre.");

            modelo.Nombre = dto.Nombre;
            modelo.MarcaVehiculoId = dto.MarcaVehiculoId;
            modelo.Cilindrada = dto.Cilindrada;
            modelo.TipoMotor = dto.TipoMotor;

            await _modeloRepository.ActualizarAsync(modelo);

            return true;
        }

        public async Task<bool> EliminarAsync(int id)
        {
            var modelo = await _modeloRepository.ObtenerPorIdAsync(id);

            if (modelo == null)
                return false;

            await _modeloRepository.EliminarAsync(modelo);

            return true;
        }
    }
}