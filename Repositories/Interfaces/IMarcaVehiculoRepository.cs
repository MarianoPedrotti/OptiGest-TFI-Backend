using SistemaDeVentas.Entities;

namespace SistemaDeVentas.Repositories.Interfaces
{
    public interface IMarcaVehiculoRepository
    {
        Task<List<MarcaVehiculo>> ObtenerTodosAsync();

        Task<MarcaVehiculo?> ObtenerPorIdAsync(int id);

        Task<MarcaVehiculo?> ObtenerPorNombreAsync(string nombre);

        Task<MarcaVehiculo> CrearAsync(MarcaVehiculo marca);

        Task ActualizarAsync(MarcaVehiculo marca);

        Task EliminarAsync(MarcaVehiculo marca);
    }
}