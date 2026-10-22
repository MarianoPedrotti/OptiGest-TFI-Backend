namespace SistemaDeVentas.DTOs.SubCategorias
{
    public class SubCategoriaUpdateDto
    {
        public int Id { get; set; }

        public int CategoriaId { get; set; }

        public string Nombre { get; set; } = string.Empty;
    }
}