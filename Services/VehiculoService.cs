using SistemaDeVentas.DTOs.Vehiculos;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;
using SistemaDeVentas.Helpers;

namespace SistemaDeVentas.Services
{
    public class VehiculoService : IVehiculoService
    {
        private readonly IVehiculoRepository _vehiculoRepository;
        private readonly IModeloRepository _modeloRepository;

        public VehiculoService(
            IVehiculoRepository vehiculoRepository,
            IModeloRepository modeloRepository)
        {
            _vehiculoRepository = vehiculoRepository;
            _modeloRepository = modeloRepository;
        }

        public async Task<List<VehiculoDto>> ObtenerTodosAsync()
        {
            var vehiculos = await _vehiculoRepository.ObtenerTodosAsync();

            return vehiculos.Select(MapearDto).ToList();
        }

        public async Task<VehiculoDto?> ObtenerPorIdAsync(int id)
        {
            var vehiculo = await _vehiculoRepository.ObtenerPorIdAsync(id);

            return vehiculo == null ? null : MapearDto(vehiculo);
        }

        public async Task<List<VehiculoDto>> ObtenerPorModeloAsync(int modeloId)
        {
            var vehiculos = await _vehiculoRepository.ObtenerPorModeloAsync(modeloId);

            return vehiculos.Select(MapearDto).ToList();
        }

        public async Task<List<VehiculoDto>> ObtenerDisponiblesAsync()
        {
            var vehiculos = await _vehiculoRepository.ObtenerDisponiblesAsync();

            return vehiculos.Select(MapearDto).ToList();
        }

        public async Task<VehiculoDto> CrearAsync(
    VehiculoCreateDto dto)
        {
            var vehiculoMismoVin =
                await _vehiculoRepository
                    .ObtenerPorVinAsync(dto.VIN);

            if (vehiculoMismoVin != null)
            {
                throw new InvalidOperationException(
                    "Ya existe un vehículo con ese VIN."
                );
            }


            var vehiculoMismoMotor =
                await _vehiculoRepository
                    .ObtenerPorNumeroMotorAsync(
                        dto.NumeroMotor
                    );

            if (vehiculoMismoMotor != null)
            {
                throw new InvalidOperationException(
                    "Ya existe un vehículo con ese número de motor."
                );
            }


            if (!string.IsNullOrWhiteSpace(dto.Patente))
            {
                var vehiculoMismaPatente =
                    await _vehiculoRepository
                        .ObtenerPorPatenteAsync(
                            dto.Patente
                        );

                if (vehiculoMismaPatente != null)
                {
                    throw new InvalidOperationException(
                        "Ya existe un vehículo con esa patente."
                    );
                }
            }


            var modelo =
                await _modeloRepository
                    .ObtenerPorIdAsync(dto.ModeloId);

            if (modelo == null)
            {
                throw new ArgumentException(
                    "El modelo seleccionado no existe."
                );
            }


            var vehiculo = new Vehiculo
            {
                ModeloId = dto.ModeloId,
                Anio = dto.Anio,
                VIN = dto.VIN,
                NumeroMotor = dto.NumeroMotor,
                Color = dto.Color,
                Kilometraje = dto.Kilometraje,
                Patente = dto.Patente,
                PrecioCompra = dto.PrecioCompra,
                PrecioVenta = dto.PrecioVenta,
                Estado = "Disponible"
            };


            await _vehiculoRepository
                .CrearAsync(vehiculo);


            vehiculo =
                await _vehiculoRepository
                    .ObtenerPorIdAsync(vehiculo.Id);


            return MapearDto(vehiculo!);
        }

        public async Task<bool> ActualizarAsync(
    VehiculoUpdateDto dto,
    string rolUsuario)
        {
            var vehiculo =
                await _vehiculoRepository
                    .ObtenerPorIdAsync(dto.Id);

            if (vehiculo == null)
                return false;


            // ==========================
            // Verificar venta asociada
            // ==========================

            var tieneVenta =
                await _vehiculoRepository
                    .TieneVentaAsociadaAsync(dto.Id);


            if (tieneVenta)
            {
                if (rolUsuario == Roles.Vendedor)
                {
                    throw new InvalidOperationException(
                        "No se puede modificar un vehículo que ya posee una venta asociada."
                    );
                }


                if (dto.Estado == "Disponible")
                {
                    throw new InvalidOperationException(
                        "No se puede marcar como disponible un vehículo que posee una venta asociada."
                    );
                }
            }


            // ==========================
            // Validar modelo
            // ==========================

            var modelo =
                await _modeloRepository
                    .ObtenerPorIdAsync(dto.ModeloId);

            if (modelo == null)
            {
                throw new ArgumentException(
                    "El modelo seleccionado no existe."
                );
            }


            // ==========================
            // Validar VIN
            // ==========================

            var vehiculoMismoVin =
                await _vehiculoRepository
                    .ObtenerPorVinAsync(dto.VIN);

            if (
                vehiculoMismoVin != null &&
                vehiculoMismoVin.Id != dto.Id
            )
            {
                throw new InvalidOperationException(
                    "Ya existe otro vehículo con ese VIN."
                );
            }


            // ==========================
            // Validar número de motor
            // ==========================

            var vehiculoMismoMotor =
                await _vehiculoRepository
                    .ObtenerPorNumeroMotorAsync(
                        dto.NumeroMotor
                    );

            if (
                vehiculoMismoMotor != null &&
                vehiculoMismoMotor.Id != dto.Id
            )
            {
                throw new InvalidOperationException(
                    "Ya existe otro vehículo con ese número de motor."
                );
            }


            // ==========================
            // Validar patente
            // ==========================

            if (!string.IsNullOrWhiteSpace(dto.Patente))
            {
                var vehiculoMismaPatente =
                    await _vehiculoRepository
                        .ObtenerPorPatenteAsync(
                            dto.Patente
                        );

                if (
                    vehiculoMismaPatente != null &&
                    vehiculoMismaPatente.Id != dto.Id
                )
                {
                    throw new InvalidOperationException(
                        "Ya existe otro vehículo con esa patente."
                    );
                }
            }


            // ==========================
            // Actualizar
            // ==========================

            vehiculo.ModeloId = dto.ModeloId;
            vehiculo.Anio = dto.Anio;
            vehiculo.VIN = dto.VIN;
            vehiculo.NumeroMotor = dto.NumeroMotor;
            vehiculo.Color = dto.Color;
            vehiculo.Kilometraje = dto.Kilometraje;
            vehiculo.Patente = dto.Patente;
            vehiculo.Estado = dto.Estado;
            vehiculo.PrecioCompra = dto.PrecioCompra;
            vehiculo.PrecioVenta = dto.PrecioVenta;


            await _vehiculoRepository
                .ActualizarAsync(vehiculo);

            return true;
        }
        public async Task<bool> EliminarAsync(int id)
        {
            var vehiculo =
                await _vehiculoRepository
                    .ObtenerPorIdAsync(id);

            if (vehiculo == null)
                return false;


            var tieneVenta =
                await _vehiculoRepository
                    .TieneVentaAsociadaAsync(id);


            if (tieneVenta)
            {
                throw new InvalidOperationException(
                    "No se puede eliminar un vehículo que posee una venta asociada."
                );
            }


            await _vehiculoRepository
                .EliminarAsync(vehiculo);

            return true;
        }

        private static VehiculoDto MapearDto(Vehiculo v)
        {
            return new VehiculoDto
            {
                Id = v.Id,
                ModeloId = v.ModeloId,
                Modelo = v.Modelo.Nombre,
                MarcaVehiculoId = v.Modelo.MarcaVehiculoId,
                Marca = v.Modelo.MarcaVehiculo.Nombre,
                Anio = v.Anio,
                VIN = v.VIN,
                NumeroMotor = v.NumeroMotor,
                Color = v.Color,
                Kilometraje = v.Kilometraje,
                Patente = v.Patente,
                Estado = v.Estado,
                PrecioCompra = v.PrecioCompra,
                PrecioVenta = v.PrecioVenta
            };
        }
        public async Task<VehiculoResumenDto> ObtenerResumenAsync()
        {
            return await _vehiculoRepository.ObtenerResumenAsync();
        }
    }
}