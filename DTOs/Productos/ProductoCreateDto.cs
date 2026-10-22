using SistemaDeVentas.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SistemaDeVentas.DTOs.Productos
{
    public class ProductoCreateDto
    {
        public string Nombre { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public string SKU { get; set; } = string.Empty;

        public decimal PrecioCompra { get; set; }

        public decimal PrecioVenta { get; set; }
        public int SubCategoriaId { get; set; }
        public int MarcaProductoId { get; set; }
        public int Stock { get; set; }
        public int CategoriaId { get; set; }

        public string Modelo { get; set; } = string.Empty;
    }
}
