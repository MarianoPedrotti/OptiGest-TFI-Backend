using SistemaDeVentas.Entities;

public interface ICategoriaRepository
{
    Task<List<Categoria>> ObtenerTodosAsync();

    Task<Categoria?> ObtenerPorIdAsync(int id);

    Task<Categoria?> ObtenerPorNombreAsync(string nombre);

    Task<Categoria> CrearAsync(Categoria categoria);

    Task ActualizarAsync(Categoria categoria);

    Task EliminarAsync(Categoria categoria);
}