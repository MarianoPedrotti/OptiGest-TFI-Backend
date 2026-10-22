using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SistemaDeVentas.DTOs.Ventas;
using SistemaDeVentas.Services.Interfaces;
using System.Security.Claims;
using SistemaDeVentas.Helpers;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    [TypeFilter(typeof(FinancialExceptionFilter))]
    public class VentaController : ControllerBase
    {
        private readonly IVentaService _ventaService;

        public VentaController(IVentaService ventaService)
        {
            _ventaService = ventaService;
        }
        [HttpPost]
        public async Task<IActionResult> Crear(CrearVentaDto dto)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
            {
                return Unauthorized("No se encontró el claim del usuario");
            }

            var usuarioId = int.Parse(claim.Value);
            //    var usuarioId = int.Parse(
            //        User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var venta =
                await _ventaService.CrearAsync(dto, usuarioId);

            return Ok(venta);
        }
        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var ventas = await _ventaService.ObtenerTodosAsync();

            return Ok(ventas);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var venta = await _ventaService.ObtenerPorIdAsync(id);

            if (venta == null)
                return NotFound();

            return Ok(venta);
        }
        [HttpGet("resumen")]
        public async Task<IActionResult> ObtenerResumen()
        {
            var resumen =
                await _ventaService.ObtenerResumenAsync();

            return Ok(resumen);
        }
    }
}
