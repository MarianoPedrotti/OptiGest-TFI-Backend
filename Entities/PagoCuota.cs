using SistemaDeVentas.Entities;

namespace SistemaDeVentas.Entities
{
    public class PagoCuota
    {
        public int Id { get; set; }
        public int CuotaId { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        public string FormaPago { get; set; }

        // Navegación

        public Cuota Cuota { get; set; }
    }
}
