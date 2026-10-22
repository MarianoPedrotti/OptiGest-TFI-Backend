using SistemaDeVentas.DTOs.Vehiculos;
using SistemaDeVentas.Entities;

namespace SistemaDeVentas.Repositories.Interfaces
{
    public interface IVehiculoRepository
    {
        Task<Vehiculo?> ObtenerParaVentaAsync(int id);
        Task MarcarVendidoAsync(int id);
        Task<List<Vehiculo>> ObtenerTodosAsync();

        Task<Vehiculo?> ObtenerPorIdAsync(int id);

        Task<Vehiculo?> ObtenerPorVinAsync(string vin);

        Task<Vehiculo?> ObtenerPorNumeroMotorAsync(string numeroMotor);

        Task<Vehiculo?> ObtenerPorPatenteAsync(string patente);

        Task<List<Vehiculo>> ObtenerPorModeloAsync(int modeloId);

        Task<List<Vehiculo>> ObtenerDisponiblesAsync();

        Task<Vehiculo> CrearAsync(Vehiculo vehiculo);

        Task ActualizarAsync(Vehiculo vehiculo);

        Task EliminarAsync(Vehiculo vehiculo);
        Task<int> ContarDisponiblesAsync();
        Task<VehiculoResumenDto> ObtenerResumenAsync();
        Task<bool> TieneVentaAsociadaAsync(int vehiculoId);
    }
}
