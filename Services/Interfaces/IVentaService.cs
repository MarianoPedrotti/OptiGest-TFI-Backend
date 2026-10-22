using SistemaDeVentas.DTOs.Ventas;

namespace SistemaDeVentas.Services.Interfaces
{
    public interface IVentaService
    {
        Task<List<VentaDto>> ObtenerTodosAsync();

        Task<VentaDto?> ObtenerPorIdAsync(int id);

        Task<VentaDto> CrearAsync(
            CrearVentaDto dto,
            int usuarioId);
        Task<VentaResumenDto> ObtenerResumenAsync();
    }
}
