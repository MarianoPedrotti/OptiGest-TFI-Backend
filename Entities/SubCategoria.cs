namespace SistemaDeVentas.Entities
{
    public class SubCategoria
    {
        public int Id { get; set; }

        public int CategoriaId { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public Categoria Categoria { get; set; }
        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}
