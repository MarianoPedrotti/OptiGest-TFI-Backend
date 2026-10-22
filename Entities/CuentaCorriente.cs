namespace SistemaDeVentas.Entities
{
    public class CuentaCorriente
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public DateTime Fecha { get; set; }
        public string Concepto { get; set; }
        public decimal Debe { get; set; }
        public decimal Haber { get; set; }
        public decimal Saldo { get; set; }

        // Navegación

        public Cliente Cliente { get; set; }
    }
}
