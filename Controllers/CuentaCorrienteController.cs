using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class CuentaCorrienteController : ControllerBase
    {
        private readonly ICuentaCorrienteService _cuentaCorrienteService;

        public CuentaCorrienteController(
            ICuentaCorrienteService cuentaCorrienteService)
        {
            _cuentaCorrienteService =
                cuentaCorrienteService;
        }

        [HttpGet("{clienteId}")]
        public async Task<IActionResult>
            ObtenerCuenta(int clienteId)
        {
            var cuenta =
                await _cuentaCorrienteService
                    .ObtenerCuentaClienteAsync(clienteId);

            return Ok(cuenta);
        }
        [HttpGet]
        public async Task<IActionResult> ObtenerResumen()
        {
            var resumen =
                await _cuentaCorrienteService.ObtenerResumenAsync();

            return Ok(resumen);
        }
    }
}

