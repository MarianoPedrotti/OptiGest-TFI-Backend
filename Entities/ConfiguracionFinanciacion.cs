namespace SistemaDeVentas.Entities
{
    public class ConfiguracionFinanciacion
    {
        public int Id { get; set; }
        public int CantidadCuotas { get; set; }
        public decimal TasaInteres { get; set; }
        public decimal MontoMinimo { get; set; }
        public decimal MontoMaximo { get; set; }
        public bool Activa { get; set; }
        // Navegación

        public ICollection<Financiacion> Financiaciones { get; set; } = new List<Financiacion>();
    }
}

