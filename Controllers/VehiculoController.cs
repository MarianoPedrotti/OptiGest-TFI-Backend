using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDeVentas.DTOs.Vehiculos;
using SistemaDeVentas.Services.Interfaces;
using System.Security.Claims;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VehiculoController : ControllerBase
    {
        private readonly IVehiculoService _vehiculoService;


        public VehiculoController(
            IVehiculoService vehiculoService)
        {
            _vehiculoService = vehiculoService;
        }


        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var vehiculos =
                await _vehiculoService
                    .ObtenerTodosAsync();

            return Ok(vehiculos);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(
            int id)
        {
            var vehiculo =
                await _vehiculoService
                    .ObtenerPorIdAsync(id);

            if (vehiculo == null)
                return NotFound();

            return Ok(vehiculo);
        }


        [HttpGet("modelo/{modeloId}")]
        public async Task<IActionResult> ObtenerPorModelo(
            int modeloId)
        {
            var vehiculos =
                await _vehiculoService
                    .ObtenerPorModeloAsync(modeloId);

            return Ok(vehiculos);
        }


        [HttpGet("disponibles")]
        public async Task<IActionResult> ObtenerDisponibles()
        {
            var vehiculos =
                await _vehiculoService
                    .ObtenerDisponiblesAsync();

            return Ok(vehiculos);
        }


        [HttpPost]
        public async Task<IActionResult> Crear(
            [FromBody] VehiculoCreateDto dto)
        {
            try
            {
                var vehiculo =
                    await _vehiculoService
                        .CrearAsync(dto);

                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { id = vehiculo.Id },
                    vehiculo
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
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
            [FromRoute] int id,
            [FromBody] VehiculoUpdateDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new
                {
                    message =
                        "El Id de la URL no coincide con el del cuerpo."
                });
            }


            var rol =
                User.FindFirst(
                    ClaimTypes.Role
                )?.Value;


            if (string.IsNullOrWhiteSpace(rol))
            {
                return Unauthorized(new
                {
                    message =
                        "No se pudo determinar el rol del usuario."
                });
            }


            try
            {
                var actualizado =
                    await _vehiculoService
                        .ActualizarAsync(
                            dto,
                            rol
                        );

                if (!actualizado)
                    return NotFound();

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
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
            try
            {
                var eliminado =
                    await _vehiculoService
                        .EliminarAsync(id);

                if (!eliminado)
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


        [HttpGet("resumen")]
        public async Task<IActionResult> ObtenerResumen()
        {
            var resumen =
                await _vehiculoService
                    .ObtenerResumenAsync();

            return Ok(resumen);
        }
    }
}