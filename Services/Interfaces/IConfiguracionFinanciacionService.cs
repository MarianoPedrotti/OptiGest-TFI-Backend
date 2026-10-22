using SistemaDeVentas.DTOs.ConfiguracionFinanciacion;
using SistemaDeVentas.DTOs.Financiaciones;

namespace SistemaDeVentas.Services.Interfaces
{
    public interface IConfiguracionFinanciacionService
    {
        Task<List<ConfiguracionFinanciacionDto>> ObtenerTodosAsync();

        Task<List<ConfiguracionFinanciacionDto>> ObtenerActivasAsync();

        Task<ConfiguracionFinanciacionDto?> ObtenerPorIdAsync(int id);

        Task<ConfiguracionFinanciacionDto> CrearAsync(CrearConfiguracionFinanciacionDto dto);

        Task<bool> ActualizarAsync(ActualizarConfiguracionFinanciacionDto dto);

        Task<bool> EliminarAsync(int id);
    }
}