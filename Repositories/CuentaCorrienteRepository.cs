using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.DTOs.CuentasCorrientes;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;

namespace SistemaDeVentas.Repositories
{
    public class CuentaCorrienteRepository: ICuentaCorrienteRepository
    {
        private readonly AppDbContext _context;
        private readonly IOperacionFinancieraRepository _operacionRepository;

        public CuentaCorrienteRepository(AppDbContext context, IOperacionFinancieraRepository operacionRepository)
        {
            _context = context;
            _operacionRepository = operacionRepository;
        }

        public async Task<List<CuentaCorriente>>
            ObtenerPorClienteAsync(int clienteId)
        {
            return await _context.CuentasCorrientes
                .Where(c => c.ClienteId == clienteId)
                .OrderBy(c => c.Fecha)
                .ThenBy(c => c.Id)
                .ToListAsync();
        }

        public async Task<CuentaCorriente>CrearAsync(CuentaCorriente movimiento)
        {
            if (_context.Database.CurrentTransaction == null)
                return await _operacionRepository.EjecutarAsync(movimiento.ClienteId, () => CrearCoreAsync(movimiento));
            await _operacionRepository.BloquearClienteAsync(movimiento.ClienteId);
            return await CrearCoreAsync(movimiento);
        }

        private async Task<CuentaCorriente> CrearCoreAsync(CuentaCorriente movimiento)
        {
            var saldoActual = await ObtenerSaldoActualAsync(movimiento.ClienteId);

            movimiento.Saldo =
                saldoActual + movimiento.Debe - movimiento.Haber;
            await _context.CuentasCorrientes
                .AddAsync(movimiento);

            await _context.SaveChangesAsync();

            return movimiento;
        }
        public async Task<CuentaCorriente?> ObtenerUltimoMovimientoAsync(int clienteId)
        {
            return await _context.CuentasCorrientes
                .Where(c => c.ClienteId == clienteId)
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();
        }
        public async Task<decimal> ObtenerSaldoActualAsync(int clienteId)
        {
            return await _context.CuentasCorrientes
                .Where(c => c.ClienteId == clienteId)
                .SumAsync(c => c.Debe - c.Haber);
        }
        public async Task<List<ResumenCuentaCorrienteDto>> ObtenerResumenAsync()
        {
            return await _context.CuentasCorrientes

                .GroupBy(c => c.ClienteId)

                .Select(g => new ResumenCuentaCorrienteDto
                {
                    ClienteId = g.Key,

                    NombreCliente =
                        g.First().Cliente.Nombre,

                    Telefono =
                        g.First().Cliente.Telefono,

                    SaldoActual =
                        g.Sum(x => x.Debe - x.Haber),

                    UltimoMovimiento =
                        g.Max(x => x.Fecha)
                })

                .OrderByDescending(x => x.SaldoActual > 0)
                .ThenByDescending(x => x.SaldoActual)
                .ThenBy(x => x.NombreCliente)

                .ToListAsync();
        }
        public async Task<List<MovimientoCuentaCorrienteDto>> ObtenerMovimientosAsync(int clienteId,DateTime? desde,DateTime? hasta)
        {
            var query = _context.CuentasCorrientes
                .Where(c => c.ClienteId == clienteId);

            if (desde.HasValue)
                query = query.Where(c => c.Fecha >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(c => c.Fecha <= hasta.Value);

            return await query
                .OrderByDescending(c => c.Fecha)
                .ThenByDescending(c => c.Id)
                .Select(c => new MovimientoCuentaCorrienteDto
                {
                    Id = c.Id,
                    Fecha = c.Fecha,
                    Concepto = c.Concepto,
                    Debe = c.Debe,
                    Haber = c.Haber,
                    Saldo = c.Saldo
                })
                .ToListAsync();
        }
        public async Task<int> ContarClientesConDeudaAsync()
        {
            return await _context.CuentasCorrientes
                .GroupBy(c => c.ClienteId)
                .CountAsync(g => g.Sum(x => x.Debe - x.Haber) > 0);
        }
    }
}
