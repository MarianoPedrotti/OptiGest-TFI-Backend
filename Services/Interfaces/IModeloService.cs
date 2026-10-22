using SistemaDeVentas.DTOs.Modelos;

namespace SistemaDeVentas.Services.Interfaces
{
    public interface IModeloService
    {
        Task<List<ModeloDto>> ObtenerTodosAsync();

        Task<ModeloDto?> ObtenerPorIdAsync(int id);

        Task<ModeloDto?> ObtenerPorNombreAsync(string nombre);

        Task<List<ModeloDto>> ObtenerPorMarcaAsync(int marcaVehiculoId);

        Task<ModeloDto> CrearAsync(ModeloCreateDto dto);

        Task<bool> ActualizarAsync(ModeloUpdateDto dto);

        Task<bool> EliminarAsync(int id);
    }
}