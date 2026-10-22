namespace SistemaDeVentas.DTOs.Financiaciones
{
    public class FinanciacionResumenDto
    {
        public int Activas { get; set; }

        public int Canceladas { get; set; }

        public decimal TotalFinanciado { get; set; }

        public decimal SaldoPendiente { get; set; }
    }
}