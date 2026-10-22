namespace SistemaDeVentas.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public decimal Comision { get; set; }

        //Relaciones de navegacion
        public ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();
        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();

    }
}
