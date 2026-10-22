using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SistemaDeVentas.DTOs.PagosVenta;
using SistemaDeVentas.Services.Interfaces;
using SistemaDeVentas.Helpers;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    [TypeFilter(typeof(FinancialExceptionFilter))]
    public class PagoVentaController : ControllerBase
    {
        private readonly IPagoVentaService _pagoVentaService;

        public PagoVentaController(
            IPagoVentaService pagoVentaService)
        {
            _pagoVentaService = pagoVentaService;
        }

        [HttpPost]
        public async Task<IActionResult> Crear(
            PagoVentaCreateDto dto)
        {
            var pago =
                await _pagoVentaService.CrearAsync(dto);

            return Ok(pago);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var pagos =
                await _pagoVentaService.ObtenerTodosAsync();

            return Ok(pagos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var pago =
                await _pagoVentaService.ObtenerPorIdAsync(id);

            if (pago == null)
                return NotFound();

            return Ok(pago);
        }
    }
}

