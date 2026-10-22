using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SistemaDeVentas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {

        [Authorize]
        [HttpGet("perfil")]
        public IActionResult Perfil()
        {
            return Ok("Token válido");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("solo-admin")]
        public IActionResult SoloAdmin()
        {
            return Ok("Bienvenido Administrador");
        }
    }
}
