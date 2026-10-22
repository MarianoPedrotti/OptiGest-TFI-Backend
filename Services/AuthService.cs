using Microsoft.AspNetCore.Identity;
using SistemaDeVentas.DTOs.Auth;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Helpers;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;


namespace SistemaDeVentas.Services
{
    public class AuthService: IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly JwtHelper _jwtHelper;
        private readonly IPasswordHasher<Usuario> _passwordHasher;


        public AuthService(IUsuarioRepository usuarioRepository, JwtHelper jwtHelper, IPasswordHasher<Usuario> passwordHasher)
        {
            _usuarioRepository = usuarioRepository;
            _jwtHelper = jwtHelper;
            _passwordHasher = passwordHasher;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var usuario =
                await _usuarioRepository.ObtenerPorEmailAsync(
                    loginDto.Email
                );

            if (usuario == null)
                return null;

            var resultado =
                _passwordHasher.VerifyHashedPassword(
                    usuario,
                    usuario.PasswordHash,
                    loginDto.Password
                );

            if (resultado == PasswordVerificationResult.Failed)
                return null;

            return new LoginResponseDto
            {
                Token = _jwtHelper.GenerarToken(usuario),
                Nombre = usuario.Nombre,
                Rol = usuario.Role
            };
        }
        public async Task RegistrarAsync(RegisterDto registerDto)
        {
            var usuarioExistente =
                await _usuarioRepository.ObtenerPorEmailAsync(registerDto.Email);

            if (usuarioExistente != null)
                throw new Exception("Ya existe un usuario con ese email");

            var usuario = new Usuario
            {
                Nombre = registerDto.Nombre,
                Apellido = registerDto.Apellido,
                Email = registerDto.Email,
                PasswordHash = registerDto.Password,
                Role = registerDto.Role,
                Comision = registerDto.Comision
            };

            await _usuarioRepository.CrearAsync(usuario);
        }
    }
}
