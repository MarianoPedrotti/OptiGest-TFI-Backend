using SistemaDeVentas.DTOs.Financiaciones;
using SistemaDeVentas.Entities;

namespace SistemaDeVentas.Repositories.Interfaces
{
    public interface IFinanciacionRepository
    {
        Task ActualizarEstadoAsync(int id, string estado);
        Task<List<Financiacion>> ObtenerTodosAsync();

        Task<Financiacion?> ObtenerPorIdAsync(int id);

        Task<Financiacion> CrearAsync(Financiacion financiacion);

        Task ActualizarAsync(Financiacion financiacion);

        Task<int> ContarActivasAsync();

        Task<FinanciacionResumenDto> ObtenerResumenAsync();
    }
}
