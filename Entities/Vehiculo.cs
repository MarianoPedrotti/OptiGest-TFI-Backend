namespace SistemaDeVentas.Entities
{
    public class Vehiculo
    {
        public int Id { get; set; }
        public int ModeloId { get; set; }
        public int Anio { get; set; }
        public string VIN { get; set; } = string.Empty;
        public string NumeroMotor { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int Kilometraje { get; set; }
        public string Patente { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public Modelo Modelo { get; set; } = null!;
        public ICollection<DetalleVenta> DetallesVenta { get; set; } = new List<DetalleVenta>();
    }
}

