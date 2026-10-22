using SistemaDeVentas.Entities;

namespace SistemaDeVentas.Entities
{
    public class PagoVenta
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public string FormaPago { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }

        // Navegación

        public Venta Venta { get; set; }
    }
}
