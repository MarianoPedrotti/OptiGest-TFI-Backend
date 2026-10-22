using SistemaDeVentas.DTOs.CuentasCorrientes;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Services
{
    public class CuentaCorrienteService: ICuentaCorrienteService
    {
        private readonly ICuentaCorrienteRepository
            _cuentaCorrienteRepository;

        public CuentaCorrienteService(
            ICuentaCorrienteRepository cuentaCorrienteRepository)
        {
            _cuentaCorrienteRepository =
                cuentaCorrienteRepository;
        }

        public async Task<CuentaCorrienteDto>
            ObtenerCuentaClienteAsync(int clienteId)
        {
            var movimientos =
                await _cuentaCorrienteRepository.ObtenerPorClienteAsync(clienteId);

            return new CuentaCorrienteDto
            {
                ClienteId = clienteId,

                SaldoActual =
                    movimientos.Sum(m => m.Debe - m.Haber),

                Movimientos =
                    movimientos.Select(m =>
                        new MovimientoCuentaCorrienteDto
                        {
                            Id = m.Id,
                            Fecha = m.Fecha,
                            Concepto = m.Concepto,
                            Debe = m.Debe,
                            Haber = m.Haber,
                            Saldo = m.Saldo
                        }).ToList()
            };
        }
        public async Task<List<ResumenCuentaCorrienteDto>> ObtenerResumenAsync()
        {
            return await _cuentaCorrienteRepository
                .ObtenerResumenAsync();
        }
        public async Task<List<MovimientoCuentaCorrienteDto>>
    ObtenerMovimientosAsync(MovimientoCuentaCorrienteFiltroDto filtro)
        {
            return await _cuentaCorrienteRepository.ObtenerMovimientosAsync(
                filtro.ClienteId,
                filtro.Desde,
                filtro.Hasta);
        }
    }
}
