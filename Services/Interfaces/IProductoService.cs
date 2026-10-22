using SistemaDeVentas.DTOs.Productos;

namespace SistemaDeVentas.Services.Interfaces
{
    public interface IProductoService
    {
        Task<List<ProductoDto>> ObtenerTodosAsync();

        Task<ProductoDto?> ObtenerPorIdAsync(int id);

        Task<ProductoDto> CrearAsync(ProductoCreateDto dto);

        Task<bool> ActualizarAsync(ProductoUpdateDto dto);

        Task<bool> EliminarAsync(int id);
        Task<ProductoResumenDto> ObtenerResumenAsync();
    }
}
