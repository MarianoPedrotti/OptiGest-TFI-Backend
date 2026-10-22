namespace SistemaDeVentas.DTOs.Auth
{
    public class RegisterDto
    {
        public string Nombre { get; set; }

        public string Apellido { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public string Role { get; set; }

        public decimal Comision { get; set; }
    }
}
