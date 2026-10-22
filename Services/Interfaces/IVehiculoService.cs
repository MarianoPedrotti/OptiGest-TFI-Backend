using SistemaDeVentas.DTOs.Vehiculos;

namespace SistemaDeVentas.Services.Interfaces
{
    public interface IVehiculoService
    {
        Task<List<VehiculoDto>> ObtenerTodosAsync();

        Task<VehiculoDto?> ObtenerPorIdAsync(int id);

        Task<List<VehiculoDto>> ObtenerPorModeloAsync(int modeloId);

        Task<List<VehiculoDto>> ObtenerDisponiblesAsync();

        Task<VehiculoDto> CrearAsync(VehiculoCreateDto dto);

        Task<bool> ActualizarAsync(VehiculoUpdateDto dto, string rolUsuario);

        Task<bool> EliminarAsync(int id);
        Task<VehiculoResumenDto> ObtenerResumenAsync();
    }
}