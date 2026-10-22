using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.Data;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;

namespace SistemaDeVentas.Repositories
{
    public class UsuarioRepository: IUsuarioRepository

    {
        private readonly AppDbContext _context;

        public UsuarioRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> ObtenerPorEmailAsync(string email)
        {
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Usuario?> ObtenerPorIdAsync(int id)
        {
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<List<Usuario>> ObtenerTodosAsync()
        {
            return await _context.Usuarios.ToListAsync();
        }

        public async Task<Usuario> CrearAsync(Usuario usuario)
        {
            await _context.Usuarios.AddAsync(usuario);

            await _context.SaveChangesAsync();

            return usuario;
        }

        public async Task ActualizarAsync(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);

            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(int id)
        {
            var usuario = await ObtenerPorIdAsync(id);

            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);

                await _context.SaveChangesAsync();
            }
        }
        public async Task<List<Usuario>>
    ObtenerPorRolAsync(string role)
        {
            return await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Role == role)
                .OrderBy(u => u.Nombre)
                .ThenBy(u => u.Apellido)
                .ToListAsync();
        }
    }
}
