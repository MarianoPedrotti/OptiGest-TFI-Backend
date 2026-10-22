namespace SistemaDeVentas.Entities
{
    public class Categoria
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public ICollection<SubCategoria> SubCategorias { get; set; } = new List<SubCategoria>();
    }
}
