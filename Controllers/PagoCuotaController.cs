using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDeVentas.DTOs.PagoCuota;
using SistemaDeVentas.Services.Interfaces;
using SistemaDeVentas.Helpers;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    [TypeFilter(typeof(FinancialExceptionFilter))]
    public class PagoCuotaController : ControllerBase
    {
        private readonly IPagoCuotaService _pagoCuotaService;

        public PagoCuotaController(
            IPagoCuotaService pagoCuotaService)
        {
            _pagoCuotaService = pagoCuotaService;
        }

        [HttpPost]
        public async Task<IActionResult> Crear(
            CrearPagoCuotaDto dto)
        {
            var pago =
                await _pagoCuotaService.CrearAsync(dto);

            return Ok(pago);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var pagos =
                await _pagoCuotaService.ObtenerTodosAsync();

            return Ok(pagos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var pago =
                await _pagoCuotaService.ObtenerPorIdAsync(id);

            if (pago == null)
            {
                return NotFound();
            }

            return Ok(pago);
        }
    }
}
