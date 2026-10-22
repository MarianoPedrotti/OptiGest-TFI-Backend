using SistemaDeVentas.Entities;

namespace SistemaDeVentas.Repositories.Interfaces
{
    public interface ICuotaRepository
    {
        Task<int?> ObtenerClienteIdAsync(int id);
        Task ActualizarEstadoAsync(int id, string estado);
        Task<List<Cuota>> ObtenerTodosAsync();

        Task<Cuota?> ObtenerPorIdAsync(int id);

        Task<Cuota> CrearAsync(Cuota cuota);

        Task ActualizarAsync(Cuota cuota);
        Task<List<Cuota>> ObtenerPorFinanciacionAsync(int financiacionId);
        Task<int> ContarVencidasAsync();

        Task<List<Cuota>> ObtenerProximasAsync(int cantidad);
    }
}
