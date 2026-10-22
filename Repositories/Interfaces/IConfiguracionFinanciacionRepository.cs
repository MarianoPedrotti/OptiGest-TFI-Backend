using SistemaDeVentas.Entities;

namespace SistemaDeVentas.Repositories.Interfaces
{
    public interface IConfiguracionFinanciacionRepository
    {
        Task<List<ConfiguracionFinanciacion>> ObtenerTodosAsync();

        Task<List<ConfiguracionFinanciacion>> ObtenerActivasAsync();

        Task<ConfiguracionFinanciacion?> ObtenerPorIdAsync(int id);

        Task<ConfiguracionFinanciacion> CrearAsync(ConfiguracionFinanciacion configuracion);

        Task ActualizarAsync(ConfiguracionFinanciacion configuracion);

        Task EliminarAsync(ConfiguracionFinanciacion configuracion);
        Task<bool> TieneFinanciacionesAsync(int id);
    }
}