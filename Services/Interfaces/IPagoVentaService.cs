using SistemaDeVentas.DTOs.PagosVenta;

namespace SistemaDeVentas.Services.Interfaces
{
    public interface IPagoVentaService
    {
        Task<PagoVentaDto> CrearAsync(PagoVentaCreateDto dto);

        Task<List<PagoVentaDto>> ObtenerTodosAsync();

        Task<PagoVentaDto?> ObtenerPorIdAsync(int id);
    }
}