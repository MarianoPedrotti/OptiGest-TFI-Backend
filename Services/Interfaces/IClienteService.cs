using SistemaDeVentas.DTOs.Clientes;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories;
using SistemaDeVentas.Repositories.Interfaces;


namespace SistemaDeVentas.Services.Interfaces
{
    public interface IClienteService
    {
        Task<List<ClienteDto>> ObtenerTodosAsync();

        Task<ClienteDto?> ObtenerPorIdAsync(int id);

        Task<ClienteDto> CrearAsync(
            ClienteCreateDto dto,
            int usuarioId);

        Task<bool> ActualizarAsync(ClienteUpdateDto dto);

        Task<bool> EliminarAsync(int id);
        Task<ClienteResumenDto> ObtenerResumenAsync();

    }
}
