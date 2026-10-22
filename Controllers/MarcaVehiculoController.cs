using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDeVentas.DTOs.MarcaVehiculo;
using SistemaDeVentas.Helpers;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MarcaVehiculoController : ControllerBase
    {
        private readonly IMarcaVehiculoService _marcaVehiculoService;

        public MarcaVehiculoController(IMarcaVehiculoService marcaVehiculoService)
        {
            _marcaVehiculoService = marcaVehiculoService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            return Ok(await _marcaVehiculoService.ObtenerTodosAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var marca = await _marcaVehiculoService.ObtenerPorIdAsync(id);

            if (marca == null)
                return NotFound();

            return Ok(marca);
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin + "," + Roles.Dueno)]
        public async Task<IActionResult> Crear([FromBody] MarcaVehiculoCreateDto dto)
        {
            var marca = await _marcaVehiculoService.CrearAsync(dto);

            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = marca.Id },
                marca);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.Dueno)]
        public async Task<IActionResult> Actualizar(
            int id,
            [FromBody] MarcaVehiculoUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("El Id de la URL no coincide con el del cuerpo.");

            var actualizado = await _marcaVehiculoService.ActualizarAsync(dto);

            if (!actualizado)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.Dueno)]
        public async Task<IActionResult> Eliminar(int id)
        {
            var eliminado = await _marcaVehiculoService.EliminarAsync(id);

            if (!eliminado)
                return NotFound();

            return NoContent();
        }
    }
}