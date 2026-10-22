using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDeVentas.DTOs.Usuarios;
using SistemaDeVentas.Helpers;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(
        Roles =
            Roles.Admin + "," +
            Roles.Dueno
    )]
    public class UsuarioController :
        ControllerBase
    {
        private readonly IUsuarioService
            _usuarioService;


        public UsuarioController(
            IUsuarioService usuarioService)
        {
            _usuarioService =
                usuarioService;
        }


        [HttpGet]
        public async Task<IActionResult>
            ObtenerTodos()
        {
            var usuarios =
                await _usuarioService
                    .ObtenerVendedoresAsync();

            return Ok(usuarios);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult>
            ObtenerPorId(int id)
        {
            var usuario =
                await _usuarioService
                    .ObtenerPorIdAsync(id);

            if (usuario == null)
                return NotFound();

            return Ok(usuario);
        }


        [HttpPost]
        public async Task<IActionResult>
            Crear(
                UsuarioCreateDto dto)
        {
            try
            {
                var usuario =
                    await _usuarioService
                        .CrearVendedorAsync(
                            dto
                        );

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                return BadRequest(
                    ex.Message
                );
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult>
            Actualizar(
                int id,
                UsuarioUpdateDto dto)
        {
            try
            {
                var usuario =
                    await _usuarioService
                        .ActualizarVendedorAsync(
                            id,
                            dto
                        );

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                return BadRequest(
                    ex.Message
                );
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult>
            Eliminar(int id)
        {
            try
            {
                await _usuarioService
                    .EliminarVendedorAsync(id);

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(
                    ex.Message
                );
            }
        }
    }
}