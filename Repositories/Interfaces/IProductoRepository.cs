using SistemaDeVentas.DTOs.Productos;
using SistemaDeVentas.Entities;

namespace SistemaDeVentas.Repositories.Interfaces
{
    public interface IProductoRepository
    {
        Task<Producto?> ObtenerParaVentaAsync(int id);
        Task DescontarStockAsync(int id, int cantidad);
        Task<List<Producto>> ObtenerTodosAsync();

        Task<Producto?> ObtenerPorIdAsync(int id);

        Task<Producto?> ObtenerPorSkuAsync(string sku);

        Task<Producto> CrearAsync(Producto producto);

        Task ActualizarAsync(Producto producto);

        Task EliminarAsync(Producto producto);
        Task<ProductoResumenDto> ObtenerResumenAsync();
    }
}
