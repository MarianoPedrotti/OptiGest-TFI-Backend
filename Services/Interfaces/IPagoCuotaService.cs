using SistemaDeVentas.DTOs.PagoCuota;

namespace SistemaDeVentas.Services.Interfaces
{
    public interface IPagoCuotaService
    {
        Task<PagoCuotaDto> CrearAsync(
            CrearPagoCuotaDto dto);

        Task<PagoCuotaDto?> ObtenerPorIdAsync(int id);

        Task<List<PagoCuotaDto>> ObtenerTodosAsync();
    }
}