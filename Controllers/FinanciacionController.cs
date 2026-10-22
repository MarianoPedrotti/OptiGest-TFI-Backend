using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDeVentas.DTOs.Financiaciones;
using SistemaDeVentas.Services.Interfaces;
using SistemaDeVentas.Helpers;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [TypeFilter(typeof(FinancialExceptionFilter))]
    [Authorize]
    public class FinanciacionController : ControllerBase
    {
        private readonly IFinanciacionService _financiacionService;

        public FinanciacionController(
            IFinanciacionService financiacionService)
        {
            _financiacionService = financiacionService;
        }

        [HttpPost]
        public async Task<IActionResult> Crear(
            CrearFinanciacionDto dto)
        {
            var financiacion =
                await _financiacionService.CrearAsync(dto);

            return Ok(financiacion);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var financiaciones =
                await _financiacionService.ObtenerTodosAsync();

            return Ok(financiaciones);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var financiacion =
                await _financiacionService.ObtenerPorIdAsync(id);

            if (financiacion == null)
            {
                return NotFound();
            }

            return Ok(financiacion);
        }
        [HttpGet("resumen")]
        public async Task<IActionResult> ObtenerResumen()
        {
            var resumen =
                await _financiacionService.ObtenerResumenAsync();

            return Ok(resumen);
        }
    }
}
