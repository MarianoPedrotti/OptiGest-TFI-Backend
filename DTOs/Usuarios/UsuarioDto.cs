namespace SistemaDeVentas.DTOs.Usuarios
{
    public class UsuarioDto
    {
        public int Id { get; set; }

        public string Nombre { get; set; } =
            string.Empty;

        public string Apellido { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public string Role { get; set; } =
            string.Empty;

        public decimal Comision { get; set; }
    }
}