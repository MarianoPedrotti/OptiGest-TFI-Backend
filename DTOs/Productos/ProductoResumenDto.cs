namespace SistemaDeVentas.DTOs.Productos
{
    public class ProductoResumenDto
    {
        public int TotalProductos { get; set; }

        public decimal ValorStockVenta { get; set; }

        public int BajoStock { get; set; }

        public int SinStock { get; set; }
    }
}