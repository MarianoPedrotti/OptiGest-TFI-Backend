namespace SistemaDeVentas.Entities
{
    public class MarcaProducto
    {
        public int Id { get; set; }

        public string Nombre { get; set; }
        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}
