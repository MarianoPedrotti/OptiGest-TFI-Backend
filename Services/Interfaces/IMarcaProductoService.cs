using SistemaDeVentas.DTOs.MarcaProducto;

public interface IMarcaProductoService
{
    Task<List<MarcaProductoDto>> ObtenerTodosAsync();

    Task<MarcaProductoDto?> ObtenerPorIdAsync(int id);

    Task<MarcaProductoDto> CrearAsync(MarcaProductoCreateDto dto);

    Task<bool> ActualizarAsync(MarcaProductoUpdateDto dto);

    Task<bool> EliminarAsync(int id);
}