using SistemaDeVentas.DTOs.Financiaciones;

namespace SistemaDeVentas.Services.Interfaces
{
    public interface IFinanciacionService
    {
        Task<FinanciacionDto> CrearAsync(
            CrearFinanciacionDto dto);

        Task<List<FinanciacionDto>> ObtenerTodosAsync();

        Task<FinanciacionDto?> ObtenerPorIdAsync(int id);

        Task<FinanciacionResumenDto> ObtenerResumenAsync();
    }
}