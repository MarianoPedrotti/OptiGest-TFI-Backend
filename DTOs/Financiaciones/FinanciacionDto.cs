using SistemaDeVentas.DTOs.Cuotas;

namespace SistemaDeVentas.DTOs.Financiaciones
{
    public class FinanciacionDto
    {
        public int Id { get; set; }

        public int VentaId { get; set; }

        public int CantidadCuotas { get; set; }

        public decimal MontoFinanciado { get; set; }

        public decimal CoeficienteAplicado { get; set; }

        public decimal MontoTotal { get; set; }

        public string Estado { get; set; }
        public string NombreCliente { get; set; } = string.Empty;

        public string NombreVehiculo { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; }

        public List<CuotaDto> Cuotas { get; set; } = new();
        public decimal MontoPagado { get; set; }

        public decimal SaldoPendiente { get; set; }

        public decimal InteresTotal { get; set; }
    }
}
