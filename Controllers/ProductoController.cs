using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDeVentas.DTOs.Productos;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _productoService;

        public ProductoController(
            IProductoService productoService)
        {
            _productoService = productoService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var productos =
                await _productoService.ObtenerTodosAsync();

            return Ok(productos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var producto =
                await _productoService.ObtenerPorIdAsync(id);

            if (producto == null)
                return NotFound();

            return Ok(producto);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(
            ProductoCreateDto dto)
        {
            try
            {
                var producto =
                    await _productoService.CrearAsync(dto);

                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { id = producto.Id },
                    producto
                );
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(
            int id,
            ProductoUpdateDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new
                {
                    message =
                        "El Id de la URL no coincide con el del cuerpo."
                });
            }

            try
            {
                var actualizado =
                    await _productoService.ActualizarAsync(dto);

                if (!actualizado)
                    return NotFound();

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var eliminado =
                await _productoService.EliminarAsync(id);

            if (!eliminado)
                return NotFound();

            return NoContent();
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> ObtenerResumen()
        {
            var resumen =
                await _productoService.ObtenerResumenAsync();

            return Ok(resumen);
        }
    }
}