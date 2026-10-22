using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDeVentas.DTOs.SubCategorias;
using SistemaDeVentas.Helpers;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubCategoriaController : ControllerBase
    {
        private readonly ISubCategoriaService _subCategoriaService;

        public SubCategoriaController(ISubCategoriaService subCategoriaService)
        {
            _subCategoriaService = subCategoriaService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var subCategorias = await _subCategoriaService.ObtenerTodosAsync();

            return Ok(subCategorias);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId([FromRoute] int id)
        {
            var subCategoria = await _subCategoriaService.ObtenerPorIdAsync(id);

            if (subCategoria == null)
                return NotFound();

            return Ok(subCategoria);
        }

        [HttpGet("nombre/{nombre}")]
        public async Task<IActionResult> ObtenerPorNombre([FromRoute] string nombre)
        {
            var subCategoria = await _subCategoriaService.ObtenerPorNombreAsync(nombre);

            if (subCategoria == null)
                return NotFound();

            return Ok(subCategoria);
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin + "," + Roles.Dueno)]
        public async Task<IActionResult> Crear([FromBody] SubCategoriaCreateDto dto)
        {
            var subCategoria = await _subCategoriaService.CrearAsync(dto);

            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = subCategoria.Id },
                subCategoria);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.Dueno)]
        public async Task<IActionResult> Actualizar(
            [FromRoute] int id,
            [FromBody] SubCategoriaUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("El Id de la URL no coincide con el del cuerpo.");

            var actualizado = await _subCategoriaService.ActualizarAsync(dto);

            if (!actualizado)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.Dueno)]
        public async Task<IActionResult> Eliminar([FromRoute] int id)
        {
            var eliminado = await _subCategoriaService.EliminarAsync(id);

            if (!eliminado)
                return NotFound();

            return NoContent();
        }
        [HttpGet("categoria/{categoriaId}")]
        public async Task<ActionResult<List<SubCategoriaDto>>> ObtenerPorCategoria(int categoriaId)
        {
            var resultado = await _subCategoriaService.ObtenerPorCategoriaAsync(categoriaId);

            return Ok(resultado);
        }

    }
}