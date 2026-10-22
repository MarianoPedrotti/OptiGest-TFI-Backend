namespace SistemaDeVentas.DTOs.Productos
{
    public class ProductoUpdateDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public decimal PrecioCompra { get; set; }

        public decimal PrecioVenta { get; set; }

        public int Stock { get; set; }
        public string SKU { get; set; } = string.Empty;

        public int CategoriaId { get; set; }
        public int SubCategoriaId { get; set; }
        public int MarcaProductoId { get; set; }

        public string Modelo { get; set; }
    }
}
