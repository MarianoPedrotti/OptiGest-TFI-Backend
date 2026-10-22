namespace SistemaDeVentas.DTOs.Ventas
{
    public class DetalleVentaDto
    {
        public int? ProductoId { get; set; }

        public int? VehiculoId { get; set; }

        public int Cantidad { get; set; }
    }
}
