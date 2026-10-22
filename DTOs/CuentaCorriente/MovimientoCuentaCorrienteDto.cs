namespace SistemaDeVentas.DTOs.CuentasCorrientes
{
    public class MovimientoCuentaCorrienteDto
    {
        public int Id { get; set; }

        public DateTime Fecha { get; set; }

        public string Concepto { get; set; }

        public decimal Debe { get; set; }

        public decimal Haber { get; set; }

        public decimal Saldo { get; set; }
    }
}