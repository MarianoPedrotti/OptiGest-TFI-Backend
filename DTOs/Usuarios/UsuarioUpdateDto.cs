namespace SistemaDeVentas.DTOs.Usuarios
{
    public class UsuarioUpdateDto
    {
        public string Nombre { get; set; } =
            string.Empty;

        public string Apellido { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public decimal Comision { get; set; }

        public string? Password { get; set; }
    }
}