using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDeVentas.DTOs.Clientes;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ClienteController : ControllerBase
    {
        private readonly IClienteService _clienteService;

        public ClienteController(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var clientes = await _clienteService.ObtenerTodosAsync();

            return Ok(clientes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var cliente = await _clienteService.ObtenerPorIdAsync(id);

            if (cliente == null)
                return NotFound();

            return Ok(cliente);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(ClienteCreateDto dto)
        {
            var usuarioId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var cliente =
                await _clienteService.CrearAsync(dto, usuarioId);

            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = cliente.Id },
                cliente);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id,ClienteUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest();

            var actualizado =
                await _clienteService.ActualizarAsync(dto);

            if (!actualizado)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var eliminado =
                await _clienteService.EliminarAsync(id);

            if (!eliminado)
                return NotFound();

            return NoContent();
        }
        [HttpGet("resumen")]
        public async Task<IActionResult> ObtenerResumen()
        {
            var resumen =
                await _clienteService.ObtenerResumenAsync();

            return Ok(resumen);
        }
    }
}