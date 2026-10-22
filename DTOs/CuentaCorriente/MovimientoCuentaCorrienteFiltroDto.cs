namespace SistemaDeVentas.DTOs.CuentasCorrientes
{
    public class MovimientoCuentaCorrienteFiltroDto
    {
        public int ClienteId { get; set; }

        public DateTime? Desde { get; set; }

        public DateTime? Hasta { get; set; }
    }
}