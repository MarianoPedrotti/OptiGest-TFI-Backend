using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDeVentas.DTOs.Modelos;
using SistemaDeVentas.Services.Interfaces;
using SistemaDeVentas.Helpers;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ModeloController : ControllerBase
    {
        private readonly IModeloService _modeloService;

        public ModeloController(IModeloService modeloService)
        {
            _modeloService = modeloService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            return Ok(await _modeloService.ObtenerTodosAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var modelo = await _modeloService.ObtenerPorIdAsync(id);

            if (modelo == null)
                return NotFound();

            return Ok(modelo);
        }

        [HttpGet("marca/{marcaVehiculoId}")]
        public async Task<IActionResult> ObtenerPorMarca(int marcaVehiculoId)
        {
            var modelos = await _modeloService.ObtenerPorMarcaAsync(marcaVehiculoId);

            return Ok(modelos);
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin + "," + Roles.Dueno)]
        public async Task<IActionResult> Crear([FromBody] ModeloCreateDto dto)
        {
            var modelo = await _modeloService.CrearAsync(dto);

            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = modelo.Id },
                modelo);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.Dueno)]
        public async Task<IActionResult> Actualizar(
            int id,
            [FromBody] ModeloUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("El Id de la URL no coincide con el del cuerpo.");

            var actualizado = await _modeloService.ActualizarAsync(dto);

            if (!actualizado)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.Dueno)]
        public async Task<IActionResult> Eliminar(int id)
        {
            var eliminado = await _modeloService.EliminarAsync(id);

            if (!eliminado)
                return NotFound();

            return NoContent();
        }
    }
}