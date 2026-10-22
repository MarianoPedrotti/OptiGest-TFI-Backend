using Microsoft.AspNetCore.Identity;
using SistemaDeVentas.DTOs.Usuarios;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Helpers;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository
            _usuarioRepository;

        private readonly IPasswordHasher<Usuario>
            _passwordHasher;


        public UsuarioService(
            IUsuarioRepository usuarioRepository,
            IPasswordHasher<Usuario> passwordHasher)
        {
            _usuarioRepository =
                usuarioRepository;

            _passwordHasher =
                passwordHasher;
        }


        public async Task<List<UsuarioDto>>
            ObtenerVendedoresAsync()
        {
            var usuarios =
                await _usuarioRepository
                    .ObtenerPorRolAsync(
                        Roles.Vendedor
                    );

            return usuarios
                .Select(MapearDto)
                .ToList();
        }


        public async Task<UsuarioDto?>
            ObtenerPorIdAsync(int id)
        {
            var usuario =
                await _usuarioRepository
                    .ObtenerPorIdAsync(id);

            if (usuario == null)
                return null;

            if (usuario.Role != Roles.Vendedor)
                return null;

            return MapearDto(usuario);
        }


        public async Task<UsuarioDto>
            CrearVendedorAsync(
                UsuarioCreateDto dto)
        {
            if (
                string.IsNullOrWhiteSpace(
                    dto.Nombre
                )
            )
            {
                throw new Exception(
                    "El nombre es obligatorio."
                );
            }


            if (
                string.IsNullOrWhiteSpace(
                    dto.Email
                )
            )
            {
                throw new Exception(
                    "El email es obligatorio."
                );
            }


            if (
                string.IsNullOrWhiteSpace(
                    dto.Password
                )
            )
            {
                throw new Exception(
                    "La contraseña es obligatoria."
                );
            }


            var existente =
                await _usuarioRepository
                    .ObtenerPorEmailAsync(
                        dto.Email
                    );

            if (existente != null)
            {
                throw new Exception(
                    "Ya existe un usuario con ese email."
                );
            }


            var usuario = new Usuario
            {
                Nombre = dto.Nombre.Trim(),

                Apellido = dto.Apellido.Trim(),

                Email = dto.Email
                    .Trim()
                    .ToLower(),

                Role = Roles.Vendedor,

                Comision = dto.Comision
            };


            usuario.PasswordHash =
                _passwordHasher.HashPassword(
                    usuario,
                    dto.Password
                );


            var creado =
                await _usuarioRepository
                    .CrearAsync(usuario);


            return MapearDto(creado);
        }


        public async Task<UsuarioDto>
            ActualizarVendedorAsync(
                int id,
                UsuarioUpdateDto dto)
        {
            var usuario =
                await _usuarioRepository
                    .ObtenerPorIdAsync(id);

            if (usuario == null)
            {
                throw new Exception(
                    "Usuario no encontrado."
                );
            }


            if (
                usuario.Role !=
                Roles.Vendedor
            )
            {
                throw new Exception(
                    "Solo se pueden modificar usuarios vendedores."
                );
            }


            var usuarioMismoEmail =
                await _usuarioRepository
                    .ObtenerPorEmailAsync(
                        dto.Email
                    );

            if (
                usuarioMismoEmail != null &&
                usuarioMismoEmail.Id != id
            )
            {
                throw new Exception(
                    "Ya existe otro usuario con ese email."
                );
            }


            usuario.Nombre =
                dto.Nombre.Trim();

            usuario.Apellido =
                dto.Apellido.Trim();

            usuario.Email =
                dto.Email
                    .Trim()
                    .ToLower();

            usuario.Comision =
                dto.Comision;


            if (
                !string.IsNullOrWhiteSpace(
                    dto.Password
                )
            )
            {
                usuario.PasswordHash =
                    _passwordHasher.HashPassword(
                        usuario,
                        dto.Password
                    );
            }


            // Nunca permitimos cambiar
            // el rol desde este CRUD.
            usuario.Role =
                Roles.Vendedor;


            await _usuarioRepository
                .ActualizarAsync(usuario);


            return MapearDto(usuario);
        }


        public async Task
            EliminarVendedorAsync(int id)
        {
            var usuario =
                await _usuarioRepository
                    .ObtenerPorIdAsync(id);

            if (usuario == null)
            {
                throw new Exception(
                    "Usuario no encontrado."
                );
            }


            if (
                usuario.Role !=
                Roles.Vendedor
            )
            {
                throw new Exception(
                    "Solo se pueden eliminar usuarios vendedores."
                );
            }


            await _usuarioRepository
                .EliminarAsync(id);
        }


        private static UsuarioDto
            MapearDto(Usuario usuario)
        {
            return new UsuarioDto
            {
                Id = usuario.Id,

                Nombre = usuario.Nombre,

                Apellido = usuario.Apellido,

                Email = usuario.Email,

                Role = usuario.Role,

                Comision = usuario.Comision
            };
        }
    }
}