namespace SistemaDeVentas.Entities
{
    public class Cliente
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Direccion { get; set; }
        public string Dni { get; set; }

        // Relaciones de navegacion
        public int UsuarioId { get; set; }
        public DateTime FechaAlta { get; set; }

        public Usuario Usuario { get; set; }
        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
        public ICollection<CuentaCorriente> MovimientosCuentaCorriente { get; set; } = new List<CuentaCorriente>();
    }
}

