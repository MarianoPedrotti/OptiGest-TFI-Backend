using SistemaDeVentas.Entities;

public interface IModeloRepository
{
    Task<List<Modelo>> ObtenerTodosAsync();

    Task<Modelo?> ObtenerPorIdAsync(int id);

    Task<Modelo?> ObtenerPorNombreAsync(string nombre);

    Task<List<Modelo>> ObtenerPorMarcaAsync(int marcaVehiculoId);

    Task<Modelo> CrearAsync(Modelo modelo);

    Task ActualizarAsync(Modelo modelo);

    Task EliminarAsync(Modelo modelo);
}