using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.DTOs.Cuotas;
using SistemaDeVentas.DTOs.Financiaciones;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;
using SistemaDeVentas.Helpers;

namespace SistemaDeVentas.Services
{
    public class FinanciacionService : IFinanciacionService
    {
        private readonly IFinanciacionRepository _financiacionRepository;
        private readonly IVentaRepository _ventaRepository;
        private readonly IConfiguracionFinanciacionRepository _configuracionRepository;
        private readonly ICuotaRepository _cuotaRepository;
        private readonly ICuentaCorrienteRepository _cuentaCorrienteRepository;
        private readonly IOperacionFinancieraRepository _operacionRepository;

        public FinanciacionService(
            IFinanciacionRepository financiacionRepository,
            IVentaRepository ventaRepository,
            IConfiguracionFinanciacionRepository configuracionRepository,
            ICuotaRepository cuotaRepository,
            ICuentaCorrienteRepository cuentaCorrienteRepository,
            IOperacionFinancieraRepository operacionRepository)
        {
            _financiacionRepository = financiacionRepository;
            _ventaRepository = ventaRepository;
            _configuracionRepository = configuracionRepository;
            _cuotaRepository = cuotaRepository;
            _cuentaCorrienteRepository = cuentaCorrienteRepository;
            _operacionRepository = operacionRepository;
        }
        public async Task<FinanciacionDto> CrearAsync(
    CrearFinanciacionDto dto)
        {
            if (dto.VentaId <= 0 || dto.ConfiguracionFinanciacionId <= 0)
                throw new ReglaFinancieraException("Los identificadores deben ser positivos.", "identificador_invalido", 400);
            var clienteId = await _ventaRepository.ObtenerClienteIdAsync(dto.VentaId)
                ?? throw new ReglaFinancieraException("La venta no existe.", "venta_inexistente", 404);
            return await _operacionRepository.EjecutarAsync(clienteId, () => CrearCoreAsync(dto, clienteId));
        }

        private async Task<FinanciacionDto> CrearCoreAsync(CrearFinanciacionDto dto, int clienteId)
        {
            // ==========================
            // Obtener venta
            // ==========================

            var venta =
                await _ventaRepository
                    .ObtenerPorIdAsync(dto.VentaId);

            if (venta == null)
            {
                throw new ReglaFinancieraException("La venta no existe.", "venta_inexistente", 404);
            }

            if (venta.ClienteId != clienteId)
                throw new ReglaFinancieraException("La venta cambió. Vuelva a consultarla.", "venta_modificada");
            var detalle = venta.DetallesVenta.Count == 1 ? venta.DetallesVenta.Single() : null;
            if (detalle == null || detalle.VehiculoId == null || detalle.ProductoId != null || detalle.Cantidad != 1)
                throw new ReglaFinancieraException("Solo se financian ventas de un único vehículo, sin artículos.", "venta_no_financiable");


            if (venta.Estado == "Pagada")
            {
                throw new ReglaFinancieraException("No se puede financiar una venta pagada.", "venta_pagada");
            }


            if (venta.Financiacion != null)
            {
                throw new ReglaFinancieraException("La venta ya posee una financiación.", "financiacion_existente");
            }


            // ==========================
            // Obtener configuración
            // ==========================

            var configuracion =
                await _configuracionRepository
                    .ObtenerPorIdAsync(
                        dto.ConfiguracionFinanciacionId
                    );

            if (configuracion == null)
            {
                throw new ReglaFinancieraException("La configuración de financiación no existe.", "plan_inexistente", 404);
            }


            if (!configuracion.Activa)
            {
                throw new ReglaFinancieraException("La configuración se encuentra inactiva.", "plan_inactivo");
            }


            // ==========================
            // Calcular saldo pendiente
            // ==========================

            var totalPagado =
                venta.Pagos.Sum(p => p.Monto);


            var saldoPendiente =
                Math.Round(
                    venta.Total - totalPagado,
                    2,
                    MidpointRounding.AwayFromZero
                );


            if (saldoPendiente <= 0)
            {
                throw new ReglaFinancieraException("La venta no posee saldo pendiente para financiar.", "venta_sin_saldo");
            }

            if (saldoPendiente < configuracion.MontoMinimo || saldoPendiente > configuracion.MontoMaximo)
                throw new ReglaFinancieraException("El saldo pendiente no entra en el rango permitido.", "saldo_fuera_de_rango");
            if (configuracion.CantidadCuotas <= 0 || configuracion.TasaInteres < 0)
                throw new ReglaFinancieraException("El plan tiene condiciones inválidas.", "plan_invalido");
            try { _ = DateTime.Now.AddMonths(configuracion.CantidadCuotas); }
            catch (ArgumentOutOfRangeException)
            { throw new ReglaFinancieraException("El plan genera vencimientos fuera del rango admitido.", "plan_invalido"); }


            // ==========================
            // Calcular financiación
            // ==========================

            var coeficiente =
                1 + (
                    configuracion.TasaInteres / 100
                );


            var montoFinanciado =
                Math.Round(
                    saldoPendiente,
                    2,
                    MidpointRounding.AwayFromZero
                );


            var montoTotal =
                Math.Round(
                    montoFinanciado * coeficiente,
                    2,
                    MidpointRounding.AwayFromZero
                );

            // Validate the existing rounding formula before any persistence.
            var montoCuotaBase = Math.Round(montoTotal / configuracion.CantidadCuotas, 2, MidpointRounding.AwayFromZero);
            var ultimaCuota = Math.Round(montoTotal - montoCuotaBase * (configuracion.CantidadCuotas - 1), 2, MidpointRounding.AwayFromZero);
            if (montoTotal > 9999999999999999.99m || ultimaCuota <= 0 || (configuracion.CantidadCuotas > 1 && montoCuotaBase <= 0))
                throw new ReglaFinancieraException("El plan no permite representar cuotas positivas con precisión monetaria.", "plan_no_representable");


            // ==========================
            // Crear financiación
            // ==========================

            var financiacion =
                new Financiacion
                {
                    VentaId =
                        venta.Id,

                    ConfiguracionFinanciacionId =
                        configuracion.Id,

                    CantidadCuotas =
                        configuracion.CantidadCuotas,

                    MontoFinanciado =
                        montoFinanciado,

                    CoeficienteAplicado =
                        coeficiente,

                    MontoTotal =
                        montoTotal,

                    FechaCreacion =
                        DateTime.Now,

                    Estado =
                        "Activa"
                };


            await _financiacionRepository
                .CrearAsync(financiacion);


            // ==========================
            // Registrar interés en
            // cuenta corriente
            // ==========================

            var incrementoDeuda =
                Math.Round(
                    montoTotal - montoFinanciado,
                    2,
                    MidpointRounding.AwayFromZero
                );


            if (incrementoDeuda > 0)
            {
                await _cuentaCorrienteRepository
                    .CrearAsync(
                        new CuentaCorriente
                        {
                            ClienteId =
                                venta.ClienteId,

                            Fecha =
                                DateTime.Now,

                            Concepto =
                                $"Financiación Venta #{venta.Id}",

                            Debe =
                                incrementoDeuda,

                            Haber =
                                0
                        }
                    );
            }


            // ==========================
            // Calcular cuotas
            // ==========================

            decimal totalCuotasGeneradas = 0;


            // ==========================
            // Generar cuotas
            // ==========================

            for (
                int i = 1;
                i <= configuracion.CantidadCuotas;
                i++
            )
            {
                decimal montoCuota;


                // La última cuota absorbe
                // cualquier diferencia de centavos
                if (
                    i == configuracion.CantidadCuotas
                )
                {
                    montoCuota =
                        montoTotal -
                        totalCuotasGeneradas;
                }
                else
                {
                    montoCuota =
                        montoCuotaBase;
                }


                montoCuota =
                    Math.Round(
                        montoCuota,
                        2,
                        MidpointRounding.AwayFromZero
                    );


                var cuota =
                    new Cuota
                    {
                        FinanciacionId =
                            financiacion.Id,

                        Numero =
                            i,

                        FechaVencimiento =
                            DateTime.Now.AddMonths(i),

                        Monto =
                            montoCuota,

                        Estado =
                            "Pendiente"
                    };


                await _cuotaRepository
                    .CrearAsync(cuota);


                totalCuotasGeneradas +=
                    montoCuota;
            }


            // ==========================
            // Respuesta
            // ==========================

            return new FinanciacionDto
            {
                Id =
                    financiacion.Id,

                VentaId =
                    financiacion.VentaId,

                CantidadCuotas =
                    financiacion.CantidadCuotas,

                MontoFinanciado =
                    financiacion.MontoFinanciado,

                CoeficienteAplicado =
                    financiacion.CoeficienteAplicado,

                MontoTotal =
                    financiacion.MontoTotal,

                Estado =
                    financiacion.Estado,

                FechaCreacion =
                    financiacion.FechaCreacion
            };
        }

        public async Task<FinanciacionDto?> ObtenerPorIdAsync(int id)
        {
            var financiacion =
                await _financiacionRepository.ObtenerPorIdAsync(id);

            if (financiacion == null)
            {
                return null;
            }

            return new FinanciacionDto
            {
                Id = financiacion.Id,

                VentaId = financiacion.VentaId,

                CantidadCuotas = financiacion.CantidadCuotas,

                MontoFinanciado = financiacion.MontoFinanciado,

                CoeficienteAplicado = financiacion.CoeficienteAplicado,

                MontoTotal = financiacion.MontoTotal,

                Estado = financiacion.Estado,

                FechaCreacion = financiacion.FechaCreacion,

                Cuotas = financiacion.Cuotas.Select(c => new CuotaDto
                {
                    Id = c.Id,
                    Numero = c.Numero,
                    FechaVencimiento = c.FechaVencimiento,
                    Monto = c.Monto,
                    Estado = c.Estado
                }).ToList(),
                NombreCliente = financiacion.Venta.Cliente.Nombre,
                NombreVehiculo = financiacion.Venta.DetallesVenta
                                    .Where(d => d.VehiculoId != null)
                                    .Select(d => d.Vehiculo.Modelo.MarcaVehiculo.Nombre + " " +
                                    d.Vehiculo.Modelo.Nombre)
                                    .FirstOrDefault() ?? string.Empty,
            };
        }

        public async Task<List<FinanciacionDto>> ObtenerTodosAsync()
        {
            var financiaciones =
                await _financiacionRepository.ObtenerTodosAsync();

            return financiaciones.Select(f => new FinanciacionDto
            {
                Id = f.Id,

                VentaId = f.VentaId,

                CantidadCuotas = f.CantidadCuotas,

                MontoFinanciado = f.MontoFinanciado,

                CoeficienteAplicado = f.CoeficienteAplicado,

                MontoTotal = f.MontoTotal,

                Estado = f.Estado,

                FechaCreacion = f.FechaCreacion,

                NombreCliente = f.Venta.Cliente.Nombre,

                NombreVehiculo = f.Venta.DetallesVenta
                    .Where(d => d.VehiculoId != null)
                    .Select(d => d.Vehiculo.Modelo.MarcaVehiculo.Nombre + " " +
                                 d.Vehiculo.Modelo.Nombre)
                    .FirstOrDefault() ?? string.Empty,

                Cuotas = f.Cuotas.Select(c => new CuotaDto
                {
                    Id = c.Id,
                    Numero = c.Numero,
                    FechaVencimiento = c.FechaVencimiento,
                    Monto = c.Monto,
                    Estado = c.Estado
                }).ToList()

            }).ToList();
        }
        public async Task<FinanciacionResumenDto> ObtenerResumenAsync()
        {
            return await _financiacionRepository
                .ObtenerResumenAsync();
        }
    }
}
