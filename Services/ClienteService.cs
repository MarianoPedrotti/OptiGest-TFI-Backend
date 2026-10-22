using SistemaDeVentas.DTOs.Clientes;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;

namespace SistemaDeVentas.Services
{
    public class ClienteService: IClienteService
    {
        private readonly IClienteRepository _clienteRepository;
        public ClienteService(IClienteRepository clienteRepository)
        {
            _clienteRepository = clienteRepository;
        }
        public async Task<List<ClienteDto>> ObtenerTodosAsync()
        {
            var clientes = await _clienteRepository.ObtenerTodosAsync();

            return clientes.Select(c => new ClienteDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Telefono = c.Telefono,
                Email = c.Email,
                Direccion = c.Direccion,
                Dni = c.Dni
            }).ToList();
        }
        public async Task<ClienteDto?> ObtenerPorIdAsync(int id)
        {
            var cliente = await _clienteRepository.ObtenerPorIdAsync(id);

            if (cliente == null)
                return null;

            return new ClienteDto
            {
                Id = cliente.Id,
                Nombre = cliente.Nombre,
                Telefono = cliente.Telefono,
                Email = cliente.Email,
                Direccion = cliente.Direccion,
                Dni = cliente.Dni
            };
        }
        public async Task<ClienteDto> CrearAsync(
    ClienteCreateDto dto,
    int usuarioId)
        {
            // Validar que el DNI sea un entero positivo
            if (!int.TryParse(dto.Dni, out var dniInt) || dniInt <= 0)
            {
                throw new ArgumentException("DNI inválido. Debe ser un número entero positivo.", nameof(dto.Dni));
            }

            var dniString = dniInt.ToString();

            // Verificar que no exista otro cliente con el mismo DNI
            var clienteExistente =
                await _clienteRepository.ObtenerPorDniAsync(dniString);

            if (clienteExistente != null)
            {
                throw new Exception("Ya existe un cliente con ese DNI");
            }

            var cliente = new Cliente
            {
                Nombre = dto.Nombre,
                Telefono = dto.Telefono,
                Email = dto.Email,
                Direccion = dto.Direccion,
                Dni = dniString,

                UsuarioId = usuarioId,
                FechaAlta = DateTime.Now
            };

            await _clienteRepository.CrearAsync(cliente);

            return new ClienteDto
            {
                Id = cliente.Id,
                Nombre = cliente.Nombre,
                Telefono = cliente.Telefono,
                Email = cliente.Email,
                Direccion = cliente.Direccion,
                Dni = cliente.Dni
            };
        }

        public async Task<bool> ActualizarAsync(ClienteUpdateDto dto)
        {
            var cliente = await _clienteRepository.ObtenerPorIdAsync(dto.Id);

            if (cliente == null)
                return false;

            // Validar que el DNI sea un entero positivo
            if (!int.TryParse(dto.Dni, out var dniInt) || dniInt <= 0)
                throw new ArgumentException("DNI inválido. Debe ser un número entero positivo.", nameof(dto.Dni));

            var dniString = dniInt.ToString();

            // Evitar duplicados: si existe otro cliente con el mismo DNI, cancelar la actualización
            var clienteConMismoDni = await _clienteRepository.ObtenerPorDniAsync(dniString);
            if (clienteConMismoDni != null && clienteConMismoDni.Id != dto.Id)
            {
                throw new Exception("Ya existe un cliente con ese DNI");
            }

            cliente.Nombre = dto.Nombre;
            cliente.Telefono = dto.Telefono;
            cliente.Email = dto.Email;
            cliente.Direccion = dto.Direccion;
            cliente.Dni = dniString;

            await _clienteRepository.ActualizarAsync(cliente);

            return true;
        }
        public async Task<bool> EliminarAsync(int id)
        {
            var cliente = await _clienteRepository.ObtenerPorIdAsync(id);

            if (cliente == null)
                return false;

            await _clienteRepository.EliminarAsync(cliente);

            return true;
        }
        public async Task<ClienteResumenDto> ObtenerResumenAsync()
        {
            return await _clienteRepository
                .ObtenerResumenAsync();
        }
    }
}
