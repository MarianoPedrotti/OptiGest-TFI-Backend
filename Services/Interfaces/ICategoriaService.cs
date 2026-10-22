using SistemaDeVentas.DTOs.Categorias;

namespace SistemaDeVentas.Services.Interfaces
{
    public interface ICategoriaService
    {
        Task<List<CategoriaDto>> ObtenerTodosAsync();

        Task<CategoriaDto?> ObtenerPorIdAsync(int id);

        Task<CategoriaDto?> ObtenerPorNombreAsync(string nombre);

        Task<CategoriaDto> CrearAsync(CategoriaCreateDto dto);

        Task<bool> ActualizarAsync(CategoriaUpdateDto dto);

        Task<bool> EliminarAsync(int id);
    }
}
