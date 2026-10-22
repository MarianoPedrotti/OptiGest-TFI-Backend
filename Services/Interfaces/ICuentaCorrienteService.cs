using SistemaDeVentas.DTOs.CuentasCorrientes;


namespace SistemaDeVentas.Services.Interfaces
{
    public interface ICuentaCorrienteService
    {
        Task<CuentaCorrienteDto> ObtenerCuentaClienteAsync(int clienteId);
        Task<List<ResumenCuentaCorrienteDto>> ObtenerResumenAsync();
        Task<List<MovimientoCuentaCorrienteDto>> ObtenerMovimientosAsync(MovimientoCuentaCorrienteFiltroDto filtro);
    }
}