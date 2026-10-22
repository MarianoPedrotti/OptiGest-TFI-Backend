namespace SistemaDeVentas.Entities
{
    public class Cuota
    {
        public int Id { get; set; }
        public int FinanciacionId { get; set; }
        public int Numero { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal Monto { get; set; }
        public string Estado { get; set; } = "Pendiente";

        // Navegación

        public Financiacion Financiacion { get; set; }

        public ICollection<PagoCuota> Pagos { get; set; } = new List<PagoCuota>();
    }
}
