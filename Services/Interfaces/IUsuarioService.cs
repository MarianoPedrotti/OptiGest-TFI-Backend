using SistemaDeVentas.DTOs.Usuarios;

namespace SistemaDeVentas.Services.Interfaces
{
    public interface IUsuarioService
    {
        Task<List<UsuarioDto>> ObtenerVendedoresAsync();

        Task<UsuarioDto?> ObtenerPorIdAsync(int id);

        Task<UsuarioDto> CrearVendedorAsync(
            UsuarioCreateDto dto
        );

        Task<UsuarioDto> ActualizarVendedorAsync(
            int id,
            UsuarioUpdateDto dto
        );

        Task EliminarVendedorAsync(int id);
    }
}