namespace SistemaDeVentas.DTOs.SubCategorias
{
    public class SubCategoriaCreateDto
    {
        public int CategoriaId { get; set; }

        public string Nombre { get; set; } = string.Empty;
    }
}