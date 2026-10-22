namespace SistemaDeVentas.DTOs.Ventas
{
    public class VentaResumenDto
    {
        public int VentasDelMes { get; set; }

        public decimal MontoVendidoMes { get; set; }

        public int Pendientes { get; set; }

        public int Pagadas { get; set; }
    }
}