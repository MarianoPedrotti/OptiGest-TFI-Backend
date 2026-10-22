using SistemaDeVentas.DTOs.SubCategorias;

namespace SistemaDeVentas.Services.Interfaces
{
    public interface ISubCategoriaService
    {
        Task<List<SubCategoriaDto>> ObtenerTodosAsync();

        Task<SubCategoriaDto?> ObtenerPorIdAsync(int id);

        Task<SubCategoriaDto?> ObtenerPorNombreAsync(string nombre);

        Task<SubCategoriaDto> CrearAsync(SubCategoriaCreateDto dto);

        Task<bool> ActualizarAsync(SubCategoriaUpdateDto dto);

        Task<bool> EliminarAsync(int id);
        Task<List<SubCategoriaDto>> ObtenerPorCategoriaAsync(int categoriaId);
    }
}