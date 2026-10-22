namespace SistemaDeVentas.DTOs.PagoCuota
{
    public class CrearPagoCuotaDto
    {
        public int CuotaId { get; set; }

        public decimal Monto { get; set; }

        public string FormaPago { get; set; }
    }
}
