namespace SistemaDeVentas.DTOs.CuentasCorrientes
{
    public class CuentaCorrienteDto
    {
        public int ClienteId { get; set; }

        public decimal SaldoActual { get; set; }

        public List<MovimientoCuentaCorrienteDto> Movimientos { get; set; }
            = new();
    }
}