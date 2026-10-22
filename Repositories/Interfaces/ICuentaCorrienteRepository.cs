using SistemaDeVentas.DTOs.CuentasCorrientes;
using SistemaDeVentas.Entities;

namespace SistemaDeVentas.Repositories.Interfaces
{
    public interface ICuentaCorrienteRepository
    {
        Task<List<CuentaCorriente>> ObtenerPorClienteAsync(int clienteId);

        Task<CuentaCorriente> CrearAsync(CuentaCorriente movimiento);
        Task<CuentaCorriente?> ObtenerUltimoMovimientoAsync(int clienteId);
        Task<decimal> ObtenerSaldoActualAsync(int clienteId);
        Task<List<ResumenCuentaCorrienteDto>> ObtenerResumenAsync();
        Task<List<MovimientoCuentaCorrienteDto>> ObtenerMovimientosAsync(int clienteId,DateTime? desde,DateTime? hasta);
        Task<int> ContarClientesConDeudaAsync();
    }
}