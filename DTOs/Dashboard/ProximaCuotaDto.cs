namespace SistemaDeVentas.DTOs.Dashboard
{
    public class ProximaCuotaDto
    {
        public int CuotaId { get; set; }

        public string Cliente { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        public DateTime FechaVencimiento { get; set; }

        public int NumeroCuota { get; set; }
    }
}