using SistemaDeVentas.DTOs.Cuotas;

namespace SistemaDeVentas.Services.Interfaces
{
    public interface ICuotaService
    {
        Task<List<CuotaDto>> ObtenerTodosAsync();

        Task<CuotaDto?> ObtenerPorIdAsync(int id);
        Task<List<CuotaDto>> ObtenerPorFinanciacionAsync(int financiacionId);
    }
}
