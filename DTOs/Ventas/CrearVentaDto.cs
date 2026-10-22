namespace SistemaDeVentas.DTOs.Ventas
{
    public class CrearVentaDto
    {
        public int ClienteId { get; set; }

        public List<DetalleVentaDto> Detalles { get; set; } = new();
        public PagoInicialVentaDto? PagoInicial { get; set; }
    }
}
