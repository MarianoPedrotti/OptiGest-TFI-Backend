using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.DTOs.Clientes;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;

namespace SistemaDeVentas.Repositories
{
    public class ClienteRepository: IClienteRepository
    {
        private readonly AppDbContext _context;

        public ClienteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Cliente>> ObtenerTodosAsync()
        {
            return await _context.Clientes.ToListAsync();
        }

        public async Task<Cliente?> ObtenerPorIdAsync(int id)
        {
            return await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Cliente?> ObtenerPorDniAsync(string dni)
        {
            return await _context.Clientes
                .FirstOrDefaultAsync(c => c.Dni == dni);
        }

        public async Task<Cliente> CrearAsync(Cliente cliente)
        {
            await _context.Clientes.AddAsync(cliente);
            await _context.SaveChangesAsync();

            return cliente;
        }

        public async Task ActualizarAsync(Cliente cliente)
        {
            _context.Clientes.Update(cliente);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(Cliente cliente)
        {
            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();
        }
        public async Task<ClienteResumenDto> ObtenerResumenAsync()
        {
            var clientes = await _context.Clientes
                .AsNoTracking()
                .Select(c => new
                {
                    Saldo = c.MovimientosCuentaCorriente
                        .Sum(m => (decimal?)(m.Debe - m.Haber)) ?? 0
                })
                .ToListAsync();


            return new ClienteResumenDto
            {
                TotalClientes =
                    clientes.Count,

                ClientesConDeuda =
                    clientes.Count(c => c.Saldo > 0),

                ClientesAlDia =
                    clientes.Count(c => c.Saldo <= 0),

                DeudaTotal =
                    clientes
                        .Where(c => c.Saldo > 0)
                        .Sum(c => c.Saldo)
            };
        }
    }
}

