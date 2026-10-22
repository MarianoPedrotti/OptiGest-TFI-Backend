namespace SistemaDeVentas.DTOs.CuentasCorrientes
{
    public class ResumenCuentaCorrienteDto
    {
        public int ClienteId { get; set; }

        public string NombreCliente { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        public decimal SaldoActual { get; set; }

        public DateTime? UltimoMovimiento { get; set; }
    }
}