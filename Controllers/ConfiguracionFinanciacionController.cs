using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDeVentas.DTOs.ConfiguracionFinanciacion;
using SistemaDeVentas.Helpers;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ConfiguracionFinanciacionController : ControllerBase
    {
        private readonly IConfiguracionFinanciacionService _service;

        public ConfiguracionFinanciacionController(
            IConfiguracionFinanciacionService service)
        {
            _service = service;
        }


        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var configuraciones =
                await _service.ObtenerTodosAsync();

            return Ok(configuraciones);
        }


        [HttpGet("activas")]
        public async Task<IActionResult> ObtenerActivas()
        {
            var configuraciones =
                await _service.ObtenerActivasAsync();

            return Ok(configuraciones);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var configuracion =
                await _service.ObtenerPorIdAsync(id);

            if (configuracion == null)
                return NotFound();

            return Ok(configuracion);
        }


        [HttpPost]
        [Authorize(Roles = Roles.Admin + "," + Roles.Dueno)]
        public async Task<IActionResult> Crear(
            CrearConfiguracionFinanciacionDto dto)
        {
            try
            {
                var configuracion =
                    await _service.CrearAsync(dto);

                return Ok(configuracion);
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
            ActualizarConfiguracionFinanciacionDto dto)
        {
            if (id != dto.Id)
                return BadRequest(
                    "El Id de la URL no coincide con el cuerpo."
                );

            try
            {
                var actualizado =
                    await _service.ActualizarAsync(dto);

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
                    await _service.EliminarAsync(id);

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