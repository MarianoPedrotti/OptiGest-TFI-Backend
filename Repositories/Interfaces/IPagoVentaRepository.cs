using SistemaDeVentas.Entities;

namespace SistemaDeVentas.Repositories.Interfaces
{
    public interface IPagoVentaRepository
    {
        Task<List<PagoVenta>> ObtenerTodosAsync();

        Task<PagoVenta?> ObtenerPorIdAsync(int id);

        Task<PagoVenta> CrearAsync(PagoVenta pago);
    }
}