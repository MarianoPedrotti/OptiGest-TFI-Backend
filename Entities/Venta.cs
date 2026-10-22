namespace SistemaDeVentas.Entities
{
    public class Venta
    {
        public int Id { get; set; }     
        public DateTime Fecha { get; set; }     
        public int ClienteId { get; set; }
        public int UsuarioId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }

        // Navegación

        public Cliente Cliente { get; set; }

        public Usuario Usuario { get; set; }

        public ICollection<DetalleVenta> DetallesVenta { get; set; } = new List<DetalleVenta>();

        public ICollection<PagoVenta> Pagos { get; set; } = new List<PagoVenta>();

        public Financiacion Financiacion { get; set; } 



    }
}
