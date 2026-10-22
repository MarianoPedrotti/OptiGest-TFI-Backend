namespace SistemaDeVentas.DTOs.Dashboard
{
    public class DashboardDto
    {
        public decimal VentasDelDia { get; set; }

        public int VehiculosDisponibles { get; set; }

        public int ClientesConDeuda { get; set; }

        public int FinanciacionesActivas { get; set; }

        public int CuotasVencidas { get; set; }

        public List<UltimaVentaDto> UltimasVentas { get; set; } = new();

        public List<ProximaCuotaDto> ProximasCuotas { get; set; } = new();
        public List<VentaDashboardDto> VentasUltimos7Dias { get; set; } = new();
    }
}