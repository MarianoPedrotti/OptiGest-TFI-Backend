using SistemaDeVentas.DTOs.Productos;

using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Services
{
    public class ProductoService: IProductoService
    {
        private readonly IProductoRepository _productoRepository;

        public ProductoService(IProductoRepository productoRepository)
        {
            _productoRepository = productoRepository;   
        }
        public async Task<List<ProductoDto>> ObtenerTodosAsync()
        {
            var productos = await _productoRepository.ObtenerTodosAsync();

            return productos.Select(p => new ProductoDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                SKU = p.SKU,
                PrecioCompra = p.PrecioCompra,
                PrecioVenta = p.PrecioVenta,
                Stock = p.Stock,
                CategoriaId = p.SubCategoria.CategoriaId,
                SubCategoriaId = p.SubCategoriaId,
                MarcaProductoId = p.MarcaProductoId,
                nombreSubCategoria = p.SubCategoria.Nombre,
                nombreMarcaProducto = p.MarcaProducto.Nombre,
                Modelo = p.Modelo
            }).ToList();
        }
        public async Task<ProductoDto?> ObtenerPorIdAsync(int id)
        {
            var producto = await _productoRepository.ObtenerPorIdAsync(id);

            if (producto == null)
                return null;

            return new ProductoDto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                SKU = producto.SKU,
                PrecioCompra = producto.PrecioCompra,
                PrecioVenta = producto.PrecioVenta,
                Stock = producto.Stock,
                CategoriaId = producto.SubCategoria.CategoriaId,
                SubCategoriaId = producto.SubCategoriaId,
                MarcaProductoId = producto.MarcaProductoId,
                nombreSubCategoria = producto.SubCategoria.Nombre,
                nombreMarcaProducto = producto.MarcaProducto.Nombre,
                Modelo = producto.Modelo
            };
        }
        public async Task<ProductoDto> CrearAsync(ProductoCreateDto dto)
        {
            var productoExistente =
                await _productoRepository.ObtenerPorSkuAsync(dto.SKU);

            if (productoExistente != null)
            {
                throw new InvalidOperationException(
    "Ya existe un producto con ese SKU.");
            }

            var producto = new Producto
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                SKU = dto.SKU,
                PrecioCompra = dto.PrecioCompra,
                PrecioVenta = dto.PrecioVenta,
                Stock = dto.Stock,
                Modelo = dto.Modelo,
                MarcaProductoId = dto.MarcaProductoId,
                SubCategoriaId = dto.SubCategoriaId
            };

            await _productoRepository.CrearAsync(producto);

            return new ProductoDto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                SKU = producto.SKU,
                PrecioCompra = producto.PrecioCompra,
                PrecioVenta = producto.PrecioVenta,
                Stock = producto.Stock,
                MarcaProductoId = dto.MarcaProductoId,
                SubCategoriaId = dto.SubCategoriaId,
                Modelo = producto.Modelo
            };
        }
        public async Task<bool> ActualizarAsync(
     ProductoUpdateDto dto)
        {
            var producto =
                await _productoRepository.ObtenerPorIdAsync(dto.Id);

            if (producto == null)
                return false;

            var productoConMismoSku =
                await _productoRepository.ObtenerPorSkuAsync(dto.SKU);

            if (
                productoConMismoSku != null &&
                productoConMismoSku.Id != dto.Id
            )
            {
                throw new InvalidOperationException(
                    "Ya existe un producto con ese SKU"
                );
            }

            producto.Nombre = dto.Nombre;
            producto.Descripcion = dto.Descripcion;
            producto.SKU = dto.SKU;
            producto.PrecioCompra = dto.PrecioCompra;
            producto.PrecioVenta = dto.PrecioVenta;
            producto.Stock = dto.Stock;
            producto.MarcaProductoId = dto.MarcaProductoId;
            producto.SubCategoriaId = dto.SubCategoriaId;
            producto.Modelo = dto.Modelo;

            await _productoRepository.ActualizarAsync(producto);

            return true;
        }
        public async Task<bool> EliminarAsync(int id)
        {
            var producto =
                await _productoRepository.ObtenerPorIdAsync(id);

            if (producto == null)
                return false;

            await _productoRepository.EliminarAsync(producto);

            return true;
        }
        public async Task<ProductoResumenDto> ObtenerResumenAsync()
        {
            return await _productoRepository
                .ObtenerResumenAsync();
        }
    }
}
