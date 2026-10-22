namespace SistemaDeVentas.DTOs.Ventas
{
    public class PagoInicialVentaDto
    {
        public string FormaPago { get; set; } = string.Empty;

        public decimal Monto { get; set; }
    }
}