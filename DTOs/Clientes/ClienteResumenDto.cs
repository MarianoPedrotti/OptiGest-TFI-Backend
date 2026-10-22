namespace SistemaDeVentas.DTOs.Clientes
{
    public class ClienteResumenDto
    {
        public int TotalClientes { get; set; }

        public int ClientesConDeuda { get; set; }

        public int ClientesAlDia { get; set; }

        public decimal DeudaTotal { get; set; }
    }
}