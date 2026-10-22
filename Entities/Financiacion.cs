namespace SistemaDeVentas.Entities
{
    public class Financiacion
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public int ConfiguracionFinanciacionId { get; set; }
        public int CantidadCuotas { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal MontoFinanciado { get; set; }
        public decimal CoeficienteAplicado { get; set; }
        public string Estado { get; set; } = "Activa";
        public DateTime FechaCreacion { get; set; }
        // Navegación

        public Venta Venta { get; set; }

        public ConfiguracionFinanciacion ConfiguracionFinanciacion { get; set; }

        public ICollection<Cuota> Cuotas { get; set; } = new List<Cuota>();

    }
}