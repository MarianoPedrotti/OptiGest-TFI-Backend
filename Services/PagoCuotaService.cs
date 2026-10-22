using SistemaDeVentas.DTOs.PagoCuota;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;
using SistemaDeVentas.Helpers;

namespace SistemaDeVentas.Services
{
    public class PagoCuotaService: IPagoCuotaService
    {
        private readonly IPagoCuotaRepository _pagoCuotaRepository;
        private readonly ICuotaRepository _cuotaRepository;
        private readonly IFinanciacionRepository _financiacionRepository;
        private readonly ICuentaCorrienteRepository _cuentaCorrienteRepository;
        private readonly IVentaRepository _ventaRepository;
        private readonly IOperacionFinancieraRepository _operacionRepository;

        public PagoCuotaService(
            IPagoCuotaRepository pagoCuotaRepository,
            ICuotaRepository cuotaRepository,
            IFinanciacionRepository financiacionRepository,
            ICuentaCorrienteRepository cuentaCorrienteRepository,
            IVentaRepository ventaRepository,
            IOperacionFinancieraRepository operacionRepository)
        {
            _pagoCuotaRepository = pagoCuotaRepository;
            _cuotaRepository = cuotaRepository;
            _financiacionRepository = financiacionRepository;
            _cuentaCorrienteRepository = cuentaCorrienteRepository;
            _ventaRepository = ventaRepository;
            _operacionRepository = operacionRepository;
        }

        public async Task<PagoCuotaDto> CrearAsync(CrearPagoCuotaDto dto)
        {
            if (dto.CuotaId <= 0 || dto.Monto <= 0 || string.IsNullOrWhiteSpace(dto.FormaPago))
                throw new ReglaFinancieraException("Indique cuota, monto positivo y forma de pago.", "pago_invalido", 400);
            var clienteId = await _cuotaRepository.ObtenerClienteIdAsync(dto.CuotaId)
                ?? throw new ReglaFinancieraException("La cuota no existe.", "cuota_inexistente", 404);
            return await _operacionRepository.EjecutarAsync(clienteId, () => CrearCoreAsync(dto, clienteId));
        }

        private async Task<PagoCuotaDto> CrearCoreAsync(CrearPagoCuotaDto dto, int clienteIdBloqueado)
        {
            var cuota =
                await _cuotaRepository.ObtenerPorIdAsync(dto.CuotaId);

            if (cuota == null)
            {
                throw new ReglaFinancieraException("La cuota no existe.", "cuota_inexistente", 404);
            }
            if (cuota.Financiacion.Venta.ClienteId != clienteIdBloqueado)
                throw new ReglaFinancieraException("La cuota cambió. Vuelva a consultarla.", "cuota_modificada");

            if (cuota.Estado == "Pagada")
            {
                throw new ReglaFinancieraException("La cuota ya se encuentra pagada.", "cuota_pagada");
            }

            if (dto.Monto <= 0)
            {
                throw new ReglaFinancieraException("El monto debe ser mayor a cero.", "monto_invalido", 400);
            }

            if (dto.Monto != cuota.Monto)
            {
                throw new ReglaFinancieraException($"La cuota debe abonarse por el monto exacto de {cuota.Monto}.", "monto_cuota_incorrecto");
            }

            var pago = new PagoCuota
            {
                CuotaId = cuota.Id,
                Fecha = DateTime.Now,
                Monto = dto.Monto,
                FormaPago = dto.FormaPago
            };

            await _pagoCuotaRepository.CrearAsync(pago);

            cuota.Estado = "Pagada";

            await _cuotaRepository.ActualizarEstadoAsync(cuota.Id, cuota.Estado);
            var financiacion =
            await _financiacionRepository.ObtenerPorIdAsync(cuota.FinanciacionId);

            if (financiacion == null)
            {
                throw new ReglaFinancieraException("La financiación no existe.", "financiacion_inexistente", 404);
            }
            if (
    financiacion.Cuotas
        .All(c => c.Estado == "Pagada")
)
            {
                financiacion.Estado = "Cancelada";

                await _financiacionRepository
                    .ActualizarEstadoAsync(financiacion.Id, financiacion.Estado);


                financiacion.Venta.Estado = "Pagada";

                await _ventaRepository
                    .ActualizarEstadoAsync(financiacion.Venta.Id, financiacion.Venta.Estado);
            }
            var clienteId =
    financiacion.Venta.ClienteId;


            await _cuentaCorrienteRepository.CrearAsync(
                new CuentaCorriente
                {
                    ClienteId = clienteId,

                    Fecha = DateTime.Now,

                    Concepto =
                        $"Pago Cuota #{cuota.Numero}",

                    Debe = 0,

                    Haber = dto.Monto,

                });

                return new PagoCuotaDto
                {
                    Id = pago.Id,

                    CuotaId = pago.CuotaId,

                    NumeroCuota = cuota.Numero,

                    FinanciacionId = financiacion.Id,

                    VentaId = financiacion.VentaId,

                    ClienteId = clienteId,

                    NombreCliente = financiacion.Venta.Cliente.Nombre,

                    Fecha = pago.Fecha,

                    Monto = pago.Monto,

                    FormaPago = pago.FormaPago
            };
        }

        public async Task<PagoCuotaDto?> ObtenerPorIdAsync(int id)
        {
            var pago = await _pagoCuotaRepository.ObtenerPorIdAsync(id);

            if (pago == null)
            {
                return null;
            }

            return new PagoCuotaDto
            {
                Id = pago.Id,

                CuotaId = pago.CuotaId,

                NumeroCuota = pago.Cuota.Numero,

                FinanciacionId = pago.Cuota.FinanciacionId,

                VentaId = pago.Cuota.Financiacion.VentaId,

                ClienteId = pago.Cuota.Financiacion.Venta.ClienteId,

                NombreCliente = pago.Cuota.Financiacion.Venta.Cliente.Nombre,

                Fecha = pago.Fecha,

                Monto = pago.Monto,

                FormaPago = pago.FormaPago
            };
        }

        public async Task<List<PagoCuotaDto>> ObtenerTodosAsync()
        {
            var pagos =
                await _pagoCuotaRepository.ObtenerTodosAsync();

            return pagos.Select(p => new PagoCuotaDto
            {
                Id = p.Id,

                CuotaId = p.CuotaId,

                NumeroCuota = p.Cuota.Numero,

                FinanciacionId = p.Cuota.FinanciacionId,

                VentaId = p.Cuota.Financiacion.VentaId,

                ClienteId = p.Cuota.Financiacion.Venta.ClienteId,

                NombreCliente = p.Cuota.Financiacion.Venta.Cliente.Nombre,

                Fecha = p.Fecha,

                Monto = p.Monto,

                FormaPago = p.FormaPago

            }).ToList();
        }
    }
}
