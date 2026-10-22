using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDeVentas.DTOs.MarcaProducto;
using SistemaDeVentas.Services.Interfaces;
using SistemaDeVentas.Helpers;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MarcaProductoController : ControllerBase
    {
        private readonly IMarcaProductoService _marcaProductoService;

        public MarcaProductoController(IMarcaProductoService marcaProductoService)
        {
            _marcaProductoService = marcaProductoService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            return Ok(await _marcaProductoService.ObtenerTodosAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var marca = await _marcaProductoService.ObtenerPorIdAsync(id);

            if (marca == null)
                return NotFound();

            return Ok(marca);
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin + "," + Roles.Dueno)]
        public async Task<IActionResult> Crear(
            [FromBody] MarcaProductoCreateDto dto)
        {
            try
            {
                var marca =
                    await _marcaProductoService.CrearAsync(dto);

                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { id = marca.Id },
                    marca
                );
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.Dueno)]
        public async Task<IActionResult> Actualizar(
     int id,
     [FromBody] MarcaProductoUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest(
                    "El Id de la URL no coincide con el del cuerpo."
                );

            try
            {
                var actualizado =
                    await _marcaProductoService.ActualizarAsync(dto);

                if (!actualizado)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.Dueno)]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                var eliminado =
                    await _marcaProductoService.EliminarAsync(id);

                if (!eliminado)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}