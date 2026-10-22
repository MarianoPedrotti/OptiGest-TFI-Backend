namespace SistemaDeVentas.DTOs.PagosVenta
{
    public class PagoVentaCreateDto
    {
        public int VentaId { get; set; }

        public string FormaPago { get; set; }

        public decimal Monto { get; set; }
    }
}