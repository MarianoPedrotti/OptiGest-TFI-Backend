using SistemaDeVentas.DTOs.MarcaVehiculo;

namespace SistemaDeVentas.Services.Interfaces
{
    public interface IMarcaVehiculoService
    {
        Task<List<MarcaVehiculoDto>> ObtenerTodosAsync();

        Task<MarcaVehiculoDto?> ObtenerPorIdAsync(int id);

        Task<MarcaVehiculoDto> CrearAsync(MarcaVehiculoCreateDto dto);

        Task<bool> ActualizarAsync(MarcaVehiculoUpdateDto dto);

        Task<bool> EliminarAsync(int id);
    }
}