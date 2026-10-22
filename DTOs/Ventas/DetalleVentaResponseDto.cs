namespace SistemaDeVentas.DTOs.Ventas
{
    public class DetalleVentaResponseDto
    {
        public int Id { get; set; }

        public int? ProductoId { get; set; }
        public string? NombreProducto { get; set; }

        public int? VehiculoId { get; set; }
        public string? NombreVehiculo { get; set; }

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal { get; set; }
    }
}