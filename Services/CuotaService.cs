using SistemaDeVentas.DTOs.Cuotas;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Services
{
    public class CuotaService : ICuotaService
    {
        private readonly ICuotaRepository _cuotaRepository;

        public CuotaService(ICuotaRepository cuotaRepository)
        {
            _cuotaRepository = cuotaRepository;
        }

        public async Task<List<CuotaDto>> ObtenerTodosAsync()
        {
            var cuotas =
                await _cuotaRepository.ObtenerTodosAsync();

            return cuotas.Select(c => new CuotaDto
            {
                Id = c.Id,

                FinanciacionId = c.FinanciacionId,

                VentaId = c.Financiacion.VentaId,

                ClienteId = c.Financiacion.Venta.ClienteId,

                NombreCliente = c.Financiacion.Venta.Cliente.Nombre,

                Numero = c.Numero,

                FechaVencimiento = c.FechaVencimiento,

                Monto = c.Monto,

                Estado = c.Estado
            }).ToList();
        }

        public async Task<CuotaDto?> ObtenerPorIdAsync(int id)
        {
            var cuota =
                await _cuotaRepository.ObtenerPorIdAsync(id);

            if (cuota == null)
            {
                return null;
            }

            return new CuotaDto
            {
                Id = cuota.Id,

                FinanciacionId = cuota.FinanciacionId,

                VentaId = cuota.Financiacion.VentaId,

                ClienteId = cuota.Financiacion.Venta.ClienteId,

                NombreCliente = cuota.Financiacion.Venta.Cliente.Nombre,

                Numero = cuota.Numero,

                FechaVencimiento = cuota.FechaVencimiento,

                Monto = cuota.Monto,

                Estado = cuota.Estado
            };
        }
        public async Task<List<CuotaDto>> ObtenerPorFinanciacionAsync(int financiacionId)
        {
            var cuotas =
                await _cuotaRepository.ObtenerPorFinanciacionAsync(financiacionId);

            return cuotas.Select(c => new CuotaDto
            {
                Id = c.Id,
                FinanciacionId = c.FinanciacionId,
                VentaId = c.Financiacion.VentaId,
                ClienteId = c.Financiacion.Venta.ClienteId,
                NombreCliente = c.Financiacion.Venta.Cliente.Nombre,
                Numero = c.Numero,
                FechaVencimiento = c.FechaVencimiento,
                Monto = c.Monto,
                Estado = c.Estado
            }).ToList();
        }
    }
}