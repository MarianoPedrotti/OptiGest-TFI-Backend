namespace SistemaDeVentas.DTOs.Cuotas
{
    public class CuotaDto
    {
        public int Id { get; set; }

        public int FinanciacionId { get; set; }

        public int VentaId { get; set; }

        public int ClienteId { get; set; }

        public string NombreCliente { get; set; }

        public int Numero { get; set; }

        public DateTime FechaVencimiento { get; set; }

        public decimal Monto { get; set; }

        public string Estado { get; set; }
    }
}