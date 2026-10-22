using SistemaDeVentas.DTOs.ConfiguracionFinanciacion;
using SistemaDeVentas.DTOs.Financiaciones;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Services
{
    public class ConfiguracionFinanciacionService : IConfiguracionFinanciacionService
    {
        private readonly IConfiguracionFinanciacionRepository _repository;

        public ConfiguracionFinanciacionService(
            IConfiguracionFinanciacionRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ConfiguracionFinanciacionDto>> ObtenerTodosAsync()
        {
            var configuraciones = await _repository.ObtenerTodosAsync();

            return configuraciones.Select(MapearDto).ToList();
        }

        public async Task<List<ConfiguracionFinanciacionDto>> ObtenerActivasAsync()
        {
            var configuraciones = await _repository.ObtenerActivasAsync();

            return configuraciones.Select(MapearDto).ToList();
        }

        public async Task<ConfiguracionFinanciacionDto?> ObtenerPorIdAsync(int id)
        {
            var configuracion = await _repository.ObtenerPorIdAsync(id);

            if (configuracion == null)
                return null;

            return MapearDto(configuracion);
        }

        public async Task<ConfiguracionFinanciacionDto> CrearAsync(CrearConfiguracionFinanciacionDto dto)
        {
                        Validar(
                dto.CantidadCuotas,
                dto.TasaInteres,
                dto.MontoMinimo,
                dto.MontoMaximo
            );
            var configuracion = new ConfiguracionFinanciacion
            {
                CantidadCuotas = dto.CantidadCuotas,
                TasaInteres = dto.TasaInteres,
                MontoMinimo = dto.MontoMinimo,
                MontoMaximo = dto.MontoMaximo,
                Activa = true
            };

            await _repository.CrearAsync(configuracion);

            return MapearDto(configuracion);
        }

        public async Task<bool> ActualizarAsync(ActualizarConfiguracionFinanciacionDto dto)
        {
                            Validar(
                    dto.CantidadCuotas,
                    dto.TasaInteres,
                    dto.MontoMinimo,
                    dto.MontoMaximo
                );
            var configuracion = await _repository.ObtenerPorIdAsync(dto.Id);

            if (configuracion == null)
                return false;

            configuracion.CantidadCuotas = dto.CantidadCuotas;
            configuracion.TasaInteres = dto.TasaInteres;
            configuracion.MontoMinimo = dto.MontoMinimo;
            configuracion.MontoMaximo = dto.MontoMaximo;
            configuracion.Activa = dto.Activa;

            await _repository.ActualizarAsync(configuracion);

            return true;
        }

        public async Task<bool> EliminarAsync(int id)
        {
            var configuracion =
                await _repository.ObtenerPorIdAsync(id);

            if (configuracion == null)
                return false;


            var fueUtilizada =
                await _repository.TieneFinanciacionesAsync(id);


            if (fueUtilizada)
            {
                throw new Exception(
                    "La configuración ya fue utilizada en una financiación. Puede desactivarla, pero no eliminarla."
                );
            }


            await _repository.EliminarAsync(
                configuracion
            );

            return true;
        }

        private static ConfiguracionFinanciacionDto MapearDto(
            ConfiguracionFinanciacion configuracion)
        {
            return new ConfiguracionFinanciacionDto
            {
                Id = configuracion.Id,
                CantidadCuotas = configuracion.CantidadCuotas,
                TasaInteres = configuracion.TasaInteres,
                MontoMinimo = configuracion.MontoMinimo,
                MontoMaximo = configuracion.MontoMaximo,
                Activa = configuracion.Activa
            };
        }
        private static void Validar(
    int cantidadCuotas,
    decimal tasaInteres,
    decimal montoMinimo,
    decimal montoMaximo)
        {
            if (cantidadCuotas <= 0)
            {
                throw new Exception(
                    "La cantidad de cuotas debe ser mayor a cero."
                );
            }

            if (tasaInteres < 0)
            {
                throw new Exception(
                    "La tasa de interés no puede ser negativa."
                );
            }

            if (montoMinimo < 0)
            {
                throw new Exception(
                    "El monto mínimo no puede ser negativo."
                );
            }

            if (montoMaximo <= 0)
            {
                throw new Exception(
                    "El monto máximo debe ser mayor a cero."
                );
            }

            if (montoMaximo < montoMinimo)
            {
                throw new Exception(
                    "El monto máximo no puede ser menor al monto mínimo."
                );
            }
        }
    }
}