using SistemaDeVentas.DTOs.Ventas;
using SistemaDeVentas.Entities;

namespace SistemaDeVentas.Repositories.Interfaces
{
    public interface IVentaRepository
    {
        Task<int?> ObtenerClienteIdAsync(int id);
        Task ActualizarEstadoAsync(int id, string estado);
        Task<List<Venta>> ObtenerTodosAsync();

        Task<Venta?> ObtenerPorIdAsync(int id);

        Task<Venta> CrearAsync(Venta venta);

        Task ActualizarAsync(Venta venta);
        Task<decimal> ObtenerVentasDelDiaAsync();

        Task<List<Venta>> ObtenerUltimasAsync(int cantidad);
        Task<List<Venta>> ObtenerVentasUltimos7DiasAsync();
        Task<VentaResumenDto> ObtenerResumenAsync();
    }
}
