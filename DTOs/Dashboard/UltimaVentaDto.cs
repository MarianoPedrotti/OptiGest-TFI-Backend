namespace SistemaDeVentas.DTOs.Dashboard
{
    public class UltimaVentaDto
    {
        public string Cliente { get; set; } = string.Empty;

        public decimal Total { get; set; }

        public DateTime Fecha { get; set; }

        public string Estado { get; set; } = string.Empty;
        public int VentaId { get; set; }
    }
}