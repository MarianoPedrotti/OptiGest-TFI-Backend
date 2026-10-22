using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SistemaDeVentas.Entities
{
    public class Producto
    {
        public int Id { get; set; }
        public int SubCategoriaId { get; set; }
        public int MarcaProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public int Stock { get; set; }
        public string Modelo { get; set; } = string.Empty;

        // Navegación
        public SubCategoria SubCategoria { get; set; }
        public MarcaProducto MarcaProducto { get; set; }
        public ICollection<DetalleVenta> DetallesVenta { get; set; } = new List<DetalleVenta>();

    }
}


