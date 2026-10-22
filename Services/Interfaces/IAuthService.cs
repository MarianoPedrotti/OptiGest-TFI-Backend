using SistemaDeVentas.DTOs.Auth;

namespace SistemaDeVentas.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
        Task RegistrarAsync(RegisterDto registerDto);
    }
}
