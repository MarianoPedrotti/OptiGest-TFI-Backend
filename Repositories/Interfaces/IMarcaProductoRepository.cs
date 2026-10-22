using SistemaDeVentas.Entities;

public interface IMarcaProductoRepository
{
    Task<List<MarcaProducto>> ObtenerTodosAsync();

    Task<MarcaProducto?> ObtenerPorIdAsync(int id);

    Task<MarcaProducto?> ObtenerPorNombreAsync(string nombre);

    Task<MarcaProducto> CrearAsync(MarcaProducto marcaProducto);

    Task ActualizarAsync(MarcaProducto marcaProducto);

    Task EliminarAsync(MarcaProducto marcaProducto);
}