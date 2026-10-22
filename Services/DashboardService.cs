using SistemaDeVentas.DTOs.Dashboard;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IVentaRepository _ventaRepository;
        private readonly IVehiculoRepository _vehiculoRepository;
        private readonly IFinanciacionRepository _financiacionRepository;
        private readonly ICuotaRepository _cuotaRepository;
        private readonly ICuentaCorrienteRepository _cuentaCorrienteRepository;

        public DashboardService(
            IVentaRepository ventaRepository,
            IVehiculoRepository vehiculoRepository,
            IFinanciacionRepository financiacionRepository,
            ICuotaRepository cuotaRepository,
            ICuentaCorrienteRepository cuentaCorrienteRepository)
        {
            _ventaRepository = ventaRepository;
            _vehiculoRepository = vehiculoRepository;
            _financiacionRepository = financiacionRepository;
            _cuotaRepository = cuotaRepository;
            _cuentaCorrienteRepository = cuentaCorrienteRepository;
        }

        public async Task<DashboardDto> ObtenerDashboardAsync()
        {
            var ventasDelDia =
                await _ventaRepository.ObtenerVentasDelDiaAsync();

            var vehiculosDisponibles =
                await _vehiculoRepository.ContarDisponiblesAsync();

            var financiacionesActivas =
                await _financiacionRepository.ContarActivasAsync();

            var clientesConDeuda =
                await _cuentaCorrienteRepository.ContarClientesConDeudaAsync();

            var cuotasVencidas =
                await _cuotaRepository.ContarVencidasAsync();

            var ultimasVentas =
                await _ventaRepository.ObtenerUltimasAsync(5);

            var proximasCuotas =
                await _cuotaRepository.ObtenerProximasAsync(5);
            var ventasUltimos7Dias =
                await _ventaRepository.ObtenerVentasUltimos7DiasAsync();
            var ventasPorDia = ventasUltimos7Dias
                .GroupBy(v => v.Fecha.Date)
                .ToDictionary(
                g => g.Key,
                g => g.Sum(v => v.Total)
                );

            var ventasUltimos7DiasDto = Enumerable
                .Range(0, 7)
                .Select(i =>
                {
                    var fecha = DateTime.Today.AddDays(-6 + i);

                    return new VentaDashboardDto
                    {
                        Fecha = fecha,
                        Total = ventasPorDia.TryGetValue(fecha, out var total)
                            ? total
                            : 0
                    };
                })
                .ToList();

            return new DashboardDto
            {
                VentasDelDia = ventasDelDia,

                VehiculosDisponibles = vehiculosDisponibles,

                ClientesConDeuda = clientesConDeuda,

                FinanciacionesActivas = financiacionesActivas,

                CuotasVencidas = cuotasVencidas,

                UltimasVentas = ultimasVentas
                    .Select(MapVenta)
                    .ToList(),

                ProximasCuotas = proximasCuotas
                    .Select(MapCuota)
                    .ToList(),
                VentasUltimos7Dias = ventasUltimos7DiasDto

            };
        }

        private static UltimaVentaDto MapVenta(Venta venta)
        {
            return new UltimaVentaDto
            {
                VentaId = venta.Id,
                Cliente = venta.Cliente.Nombre,
                Total = venta.Total,
                Fecha = venta.Fecha,
                Estado = venta.Estado
            };
        }

        private static ProximaCuotaDto MapCuota(Cuota cuota)
        {
            return new ProximaCuotaDto
            {
                CuotaId = cuota.Id,
                Cliente = cuota.Financiacion.Venta.Cliente.Nombre,
                NumeroCuota = cuota.Numero,
                FechaVencimiento = cuota.FechaVencimiento,
                Monto = cuota.Monto
            };
        }
    }
}