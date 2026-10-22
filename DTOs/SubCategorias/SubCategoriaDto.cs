namespace SistemaDeVentas.DTOs.SubCategorias
{
    public class SubCategoriaDto
    {
        public int Id { get; set; }

        public int CategoriaId { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;


        public string Nombre { get; set; } = string.Empty;
    }
}