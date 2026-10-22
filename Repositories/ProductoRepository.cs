using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.DTOs.Productos;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Helpers;

namespace SistemaDeVentas.Repositories
{
    public class ProductoRepository: IProductoRepository
    {
        private readonly AppDbContext _context;

        public ProductoRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<Producto?> ObtenerParaVentaAsync(int id)
        {
            if (_context.Database.CurrentTransaction == null)
                throw new InvalidOperationException("La lectura de existencias requiere una transacción.");
            return _context.Productos
                .FromSqlInterpolated($"SELECT * FROM [Productos] WITH (UPDLOCK, HOLDLOCK, ROWLOCK) WHERE [Id] = {id}")
                .AsNoTracking().SingleOrDefaultAsync();
        }

        public async Task DescontarStockAsync(int id, int cantidad)
        {
            if (_context.Database.CurrentTransaction == null)
                throw new InvalidOperationException("El descuento de existencias requiere una transacción.");
            if (cantidad <= 0) throw new ArgumentOutOfRangeException(nameof(cantidad));
            var affected = await _context.Productos.Where(p => p.Id == id && p.Stock >= cantidad)
                .ExecuteUpdateAsync(update => update.SetProperty(p => p.Stock, p => p.Stock - cantidad));
            if (affected != 1)
                throw new ReglaFinancieraException("Las existencias cambiaron o resultan insuficientes.", "stock_insuficiente");
        }

        public async Task<List<Producto>> ObtenerTodosAsync()
        {
            return await _context.Productos

                .Include(p => p.MarcaProducto)

                .Include(p => p.SubCategoria)

                    .ThenInclude(sc => sc.Categoria)

                .ToListAsync();
        }

        public async Task<Producto?> ObtenerPorIdAsync(int id)
        {
            return await _context.Productos
                .Include(p => p.MarcaProducto)
                .Include(p => p.SubCategoria)
                .ThenInclude(sc => sc.Categoria)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Producto?> ObtenerPorSkuAsync(string sku)
        {
            return await _context.Productos
                .Include(p => p.MarcaProducto)
                .Include(p => p.SubCategoria)
                .ThenInclude(sc => sc.Categoria)
                .FirstOrDefaultAsync(p => p.SKU == sku);
        }

        public async Task<Producto> CrearAsync(Producto producto)
        {
            await _context.Productos.AddAsync(producto);
            await _context.SaveChangesAsync();

            return producto;
        }

        public async Task ActualizarAsync(Producto producto)
        {
            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(Producto producto)
        {
            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
        }
        public async Task<ProductoResumenDto> ObtenerResumenAsync()
        {
            return new ProductoResumenDto
            {
                TotalProductos =
                    await _context.Productos.CountAsync(),

                ValorStockVenta =
                    await _context.Productos
                        .SumAsync(p =>
                            p.PrecioVenta * p.Stock
                        ),

                BajoStock =
                    await _context.Productos
                        .CountAsync(p =>
                            p.Stock > 0 &&
                            p.Stock <= 5
                        ),

                SinStock =
                    await _context.Productos
                        .CountAsync(p =>
                            p.Stock == 0
                        )
            };
        }
    }
}
