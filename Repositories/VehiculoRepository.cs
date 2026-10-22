using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.DTOs.Vehiculos;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Helpers;

namespace SistemaDeVentas.Repositories
{
    public class VehiculoRepository : IVehiculoRepository
    {
        private readonly AppDbContext _context;

        public VehiculoRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<Vehiculo?> ObtenerParaVentaAsync(int id)
        {
            if (_context.Database.CurrentTransaction == null)
                throw new InvalidOperationException("La lectura del vehículo requiere una transacción.");
            return _context.Vehiculos
                .FromSqlInterpolated($"SELECT * FROM [Vehiculos] WITH (UPDLOCK, HOLDLOCK, ROWLOCK) WHERE [Id] = {id}")
                .AsNoTracking().SingleOrDefaultAsync();
        }

        public async Task MarcarVendidoAsync(int id)
        {
            if (_context.Database.CurrentTransaction == null)
                throw new InvalidOperationException("La venta del vehículo requiere una transacción.");
            var affected = await _context.Vehiculos.Where(v => v.Id == id && v.Estado == "Disponible")
                .ExecuteUpdateAsync(update => update.SetProperty(v => v.Estado, "Vendido"));
            if (affected != 1)
                throw new ReglaFinancieraException("El vehículo ya no está disponible.", "vehiculo_no_disponible");
        }

        public async Task<List<Vehiculo>> ObtenerTodosAsync()
        {
            return await _context.Vehiculos
                .Include(v => v.Modelo)
                .ThenInclude(m => m.MarcaVehiculo)
                .OrderBy(v => v.Id)
                .ToListAsync();
        }

        public async Task<Vehiculo?> ObtenerPorIdAsync(int id)
        {
            return await _context.Vehiculos
                .Include(v => v.Modelo)
                .ThenInclude(m => m.MarcaVehiculo)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<Vehiculo?> ObtenerPorVinAsync(string vin)
        {
            return await _context.Vehiculos
                .FirstOrDefaultAsync(v => v.VIN == vin);
        }

        public async Task<Vehiculo?> ObtenerPorNumeroMotorAsync(string numeroMotor)
        {
            return await _context.Vehiculos
                .FirstOrDefaultAsync(v => v.NumeroMotor == numeroMotor);
        }

        public async Task<Vehiculo?> ObtenerPorPatenteAsync(string patente)
        {
            return await _context.Vehiculos
                .FirstOrDefaultAsync(v => v.Patente == patente);
        }

        public async Task<List<Vehiculo>> ObtenerPorModeloAsync(int modeloId)
        {
            return await _context.Vehiculos
                .Include(v => v.Modelo)
                .ThenInclude(m => m.MarcaVehiculo)
                .Where(v => v.ModeloId == modeloId)
                .OrderBy(v => v.Id)
                .ToListAsync();
        }

        public async Task<List<Vehiculo>> ObtenerDisponiblesAsync()
        {
            return await _context.Vehiculos
                .Include(v => v.Modelo)
                .ThenInclude(m => m.MarcaVehiculo)
                .Where(v => v.Estado == "Disponible")
                .OrderBy(v => v.Id)
                .ToListAsync();
        }

        public async Task<Vehiculo> CrearAsync(Vehiculo vehiculo)
        {
            await _context.Vehiculos.AddAsync(vehiculo);

            await _context.SaveChangesAsync();

            return vehiculo;
        }

        public async Task ActualizarAsync(Vehiculo vehiculo)
        {
            _context.Vehiculos.Update(vehiculo);

            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(Vehiculo vehiculo)
        {
            _context.Vehiculos.Remove(vehiculo);

            await _context.SaveChangesAsync();
        }
        public async Task<int> ContarDisponiblesAsync()
        {
            return await _context.Vehiculos
                .CountAsync(v => v.Estado == "Disponible");
        }
        public async Task<VehiculoResumenDto> ObtenerResumenAsync()
        {
            return new VehiculoResumenDto
            {
                TotalVehiculos =
                    await _context.Vehiculos.CountAsync(),

                Disponibles =
                    await _context.Vehiculos
                        .CountAsync(v => v.Estado == "Disponible"),

                Vendidos =
                    await _context.Vehiculos
                        .CountAsync(v => v.Estado == "Vendido"),

                ValorDisponibles =
                    await _context.Vehiculos
                        .Where(v => v.Estado == "Disponible")
                        .SumAsync(v => (decimal?)v.PrecioVenta) ?? 0
            };
        }
        public async Task<bool> TieneVentaAsociadaAsync(int vehiculoId)
        {
            return await _context.DetallesVenta
                .AnyAsync(d => d.VehiculoId == vehiculoId);
        }
    }
}
