using SistemaDeVentas.DTOs.Clientes;
using SistemaDeVentas.Entities;

namespace SistemaDeVentas.Repositories.Interfaces
{
    public interface IClienteRepository
    {
        Task<List<Cliente>> ObtenerTodosAsync();

        Task<Cliente?> ObtenerPorIdAsync(int id);

        Task<Cliente?> ObtenerPorDniAsync(string dni);

        Task<Cliente> CrearAsync(Cliente cliente);

        Task ActualizarAsync(Cliente cliente);

        Task EliminarAsync(Cliente cliente);

        Task<ClienteResumenDto> ObtenerResumenAsync();
    }
}
