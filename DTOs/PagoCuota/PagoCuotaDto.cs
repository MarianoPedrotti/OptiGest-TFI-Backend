namespace SistemaDeVentas.DTOs.PagoCuota
{
    public class PagoCuotaDto
    {
        public int Id { get; set; }

        public int CuotaId { get; set; }

        public int NumeroCuota { get; set; }

        public int FinanciacionId { get; set; }

        public int VentaId { get; set; }

        public int ClienteId { get; set; }

        public string NombreCliente { get; set; }

        public DateTime Fecha { get; set; }

        public decimal Monto { get; set; }

        public string FormaPago { get; set; }
    }
}