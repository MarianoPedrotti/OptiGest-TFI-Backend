using SistemaDeVentas.Entities;

namespace SistemaDeVentas.Repositories.Interfaces
{
    public interface IPagoCuotaRepository
    {
        Task<List<PagoCuota>> ObtenerTodosAsync();

        Task<PagoCuota?> ObtenerPorIdAsync(int id);

        Task<PagoCuota> CrearAsync(PagoCuota pagoCuota);

        Task ActualizarAsync(PagoCuota pagoCuota);
    }
}