using SistemaDeVentas.DTOs.PagosVenta;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;
using SistemaDeVentas.Helpers;

namespace SistemaDeVentas.Services
{
    public class PagoVentaService: IPagoVentaService
    {
        private readonly IPagoVentaRepository _pagoVentaRepository;
        private readonly IVentaRepository _ventaRepository;
        private readonly ICuentaCorrienteRepository _cuentaCorrienteRepository;
        private readonly IOperacionFinancieraRepository _operacionRepository;

        public PagoVentaService(
            IPagoVentaRepository pagoVentaRepository,
            IVentaRepository ventaRepository,
            ICuentaCorrienteRepository cuentaCorrienteRepository,
            IOperacionFinancieraRepository operacionRepository)
        {
            _pagoVentaRepository = pagoVentaRepository;
            _ventaRepository = ventaRepository;
            _cuentaCorrienteRepository = cuentaCorrienteRepository;
            _operacionRepository = operacionRepository;
        }
        public async Task<PagoVentaDto> CrearAsync(PagoVentaCreateDto dto)
        {
            if (dto.VentaId <= 0 || dto.Monto <= 0 || decimal.Round(dto.Monto, 2) != dto.Monto || string.IsNullOrWhiteSpace(dto.FormaPago))
                throw new ReglaFinancieraException("Indique venta, forma de pago y un monto positivo con hasta dos decimales.", "pago_invalido", 400);
            var clienteId = await _ventaRepository.ObtenerClienteIdAsync(dto.VentaId)
                ?? throw new ReglaFinancieraException("La venta no existe.", "venta_inexistente", 404);
            return await _operacionRepository.EjecutarAsync(clienteId, () => CrearCoreAsync(dto, clienteId));
        }

        private async Task<PagoVentaDto> CrearCoreAsync(PagoVentaCreateDto dto, int clienteId)
        {
            var venta =
                await _ventaRepository.ObtenerPorIdAsync(dto.VentaId);

            if (venta == null)
            {
                throw new ReglaFinancieraException("La venta no existe.", "venta_inexistente", 404);
            }
            if (venta.ClienteId != clienteId)
                throw new ReglaFinancieraException("La venta cambió. Vuelva a consultarla.", "venta_modificada");
            if (venta.Financiacion != null)
                throw new ReglaFinancieraException("La venta tiene financiación. Registre el cobro mediante sus cuotas.", "venta_financiada");
            if (venta.Estado == "Pagada")
            {
                throw new ReglaFinancieraException("La venta ya se encuentra totalmente pagada.", "venta_pagada");
            }
            if (dto.Monto <= 0)
            {
                throw new ReglaFinancieraException("El monto debe ser mayor a cero.", "monto_invalido", 400);
            }

            var pago = new PagoVenta
            {
                VentaId = dto.VentaId,
                FormaPago = dto.FormaPago,
                Monto = dto.Monto,
                Fecha = DateTime.Now
            };


            var totalPagado =
      venta.Pagos.Sum(p => p.Monto);

            var saldoPendiente =
                venta.Total - totalPagado;

            if (dto.Monto > saldoPendiente)
            {
                throw new ReglaFinancieraException($"El pago excede el saldo pendiente de {saldoPendiente}.", "pago_excede_saldo");
            }

            await _pagoVentaRepository.CrearAsync(pago);

            totalPagado += dto.Monto;

            if (totalPagado >= venta.Total)
            {
                venta.Estado = "Pagada";
            }
            else
            {
                venta.Estado = "PagoParcial";
            }
           
            await _cuentaCorrienteRepository.CrearAsync(new CuentaCorriente
    {
        ClienteId = venta.ClienteId,

        Fecha = DateTime.Now,

        Concepto =
            $"Pago Venta #{venta.Id}",

        Debe = 0,

        Haber = dto.Monto,

    });

            await _ventaRepository.ActualizarEstadoAsync(venta.Id, venta.Estado);

            return new PagoVentaDto
            {
                Id = pago.Id,
                VentaId = pago.VentaId,
                FormaPago = pago.FormaPago,
                Monto = pago.Monto,
                Fecha = pago.Fecha
            };
        }
        public async Task<List<PagoVentaDto>> ObtenerTodosAsync()
        {
            var pagos =
                await _pagoVentaRepository.ObtenerTodosAsync();

            return pagos.Select(p => new PagoVentaDto
            {
                Id = p.Id,
                VentaId = p.VentaId,
                FormaPago = p.FormaPago,
                Monto = p.Monto,
                Fecha = p.Fecha
            }).ToList();
        }
        public async Task<PagoVentaDto?> ObtenerPorIdAsync(int id)
        {
            var pago =
                await _pagoVentaRepository.ObtenerPorIdAsync(id);

            if (pago == null)
                return null;

            return new PagoVentaDto
            {
                Id = pago.Id,
                VentaId = pago.VentaId,
                FormaPago = pago.FormaPago,
                Monto = pago.Monto,
                Fecha = pago.Fecha
            };
        }
    }
}
