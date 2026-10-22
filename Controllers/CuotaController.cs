using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class CuotaController : ControllerBase
    {
        private readonly ICuotaService _cuotaService;

        public CuotaController(ICuotaService cuotaService)
        {
            _cuotaService = cuotaService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var cuotas =
                await _cuotaService.ObtenerTodosAsync();

            return Ok(cuotas);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var cuota =
                await _cuotaService.ObtenerPorIdAsync(id);

            if (cuota == null)
            {
                return NotFound();
            }

            return Ok(cuota);
        }
        [HttpGet("financiacion/{financiacionId}")]
        public async Task<IActionResult> ObtenerPorFinanciacion(int financiacionId)
        {
            var cuotas =
                await _cuotaService.ObtenerPorFinanciacionAsync(financiacionId);

            return Ok(cuotas);
        }
    }
}
