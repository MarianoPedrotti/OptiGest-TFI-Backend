namespace SistemaDeVentas.DTOs.Clientes
{
    public class ClienteUpdateDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }

        public string Telefono { get; set; }

        public string Email { get; set; }

        public string Direccion { get; set; }
        public string Dni { get; set; }
    }
}
