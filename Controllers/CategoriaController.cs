using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDeVentas.DTOs.Categorias;
using SistemaDeVentas.Services.Interfaces;
using SistemaDeVentas.Helpers;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoriaController : ControllerBase
    {
        private readonly ICategoriaService _categoriaService;

        public CategoriaController(ICategoriaService categoriaService)
        {
            _categoriaService = categoriaService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            return Ok(await _categoriaService.ObtenerTodosAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var categoria = await _categoriaService.ObtenerPorIdAsync(id);

            if (categoria == null)
                return NotFound();

            return Ok(categoria);
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin + "," + Roles.Dueno)]
        public async Task<IActionResult> Crear([FromBody] CategoriaCreateDto dto)
        {
            var categoria = await _categoriaService.CrearAsync(dto);

            return CreatedAtAction(nameof(ObtenerPorId), new { id = categoria.Id }, categoria);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.Dueno)]
        public async Task<IActionResult> Actualizar(int id, [FromBody] CategoriaUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest();

            var actualizado = await _categoriaService.ActualizarAsync(dto);

            if (!actualizado)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.Dueno)]
        public async Task<IActionResult> Eliminar(int id)
        {
            var eliminado = await _categoriaService.EliminarAsync(id);

            if (!eliminado)
                return NotFound();

            return NoContent();
        }
    }
}