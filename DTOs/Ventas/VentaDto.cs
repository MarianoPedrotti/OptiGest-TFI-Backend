using SistemaDeVentas.DTOs.Financiaciones;
using SistemaDeVentas.DTOs.PagosVenta;

namespace SistemaDeVentas.DTOs.Ventas
{
    public class VentaDto
    {
        public int Id { get; set; }

        public DateTime Fecha { get; set; }

        public int ClienteId { get; set; }

        public string NombreCliente { get; set; } = string.Empty;

        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }

        public decimal Total { get; set; }

        public string Estado { get; set; }
        public int? VehiculoId { get; set; }

        public string NombreVehiculo { get; set; } = string.Empty;
        public decimal PrecioVehiculo { get; set; }
        public bool TieneVehiculo { get; set; }

        public bool TieneFinanciacion { get; set; }

        public bool EstaPagada { get; set; }
        public decimal SaldoPendiente { get; set; }
        public List<DetalleVentaResponseDto> Detalles { get; set; } = new();
        public int? FinanciacionId { get; set; }
        public List<PagoVentaDto> Pagos { get; set; } = new();
        public FinanciacionDto? Financiacion { get; set; }
    }
}
