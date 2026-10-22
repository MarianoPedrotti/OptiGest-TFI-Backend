namespace SistemaDeVentas.DTOs.PagosVenta
{
    public class PagoVentaDto
    {
        public int Id { get; set; }

        public int VentaId { get; set; }

        public string FormaPago { get; set; }

        public decimal Monto { get; set; }

        public DateTime Fecha { get; set; }
    }
}