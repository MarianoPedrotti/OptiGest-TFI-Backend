using SistemaDeVentas.Entities;

namespace SistemaDeVentas.Repositories.Interfaces
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> ObtenerPorEmailAsync(string email);

        Task<Usuario?> ObtenerPorIdAsync(int id);

        Task<List<Usuario>> ObtenerTodosAsync();

        Task<Usuario> CrearAsync(Usuario usuario);

        Task ActualizarAsync(Usuario usuario);

        Task EliminarAsync(int id);
        Task<List<Usuario>> ObtenerPorRolAsync(string role);
    }
}
