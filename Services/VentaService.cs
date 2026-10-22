using SistemaDeVentas.DTOs.Financiaciones;
using SistemaDeVentas.DTOs.PagosVenta;
using SistemaDeVentas.DTOs.Ventas;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services.Interfaces;
using SistemaDeVentas.Helpers;

namespace SistemaDeVentas.Services
{
    public class VentaService: IVentaService
    {
        private readonly IVentaRepository _ventaRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IProductoRepository _productoRepository;
        private readonly ICuentaCorrienteRepository _cuentaCorrienteRepository;
        private readonly IVehiculoRepository _vehiculoRepository;
        private readonly IOperacionFinancieraRepository _operacionRepository;
        private readonly IUsuarioRepository _usuarioRepository;

        public VentaService(
            IVentaRepository ventaRepository,
            IClienteRepository clienteRepository,
            IProductoRepository productoRepository,
            ICuentaCorrienteRepository cuentaCorrienteRepository, IVehiculoRepository vehiculoRepository,
            IOperacionFinancieraRepository operacionRepository, IUsuarioRepository usuarioRepository)
        {
            _ventaRepository = ventaRepository;
            _clienteRepository = clienteRepository;
            _productoRepository = productoRepository;
            _cuentaCorrienteRepository = cuentaCorrienteRepository;
            _vehiculoRepository = vehiculoRepository;
            _operacionRepository = operacionRepository;
            _usuarioRepository = usuarioRepository;
        }

        public async Task<VentaDto> CrearAsync(CrearVentaDto dto, int usuarioId)
        {
            var clienteId = dto.ClienteId;
            if (clienteId <= 0 || usuarioId <= 0)
                throw new ReglaFinancieraException("Indique cliente y usuario válidos.", "identificador_invalido", 400);
            var detalles = NormalizarDetalles(dto.Detalles);
            var pago = dto.PagoInicial == null ? null : new PagoInicialVentaDto
            { Monto = dto.PagoInicial.Monto, FormaPago = dto.PagoInicial.FormaPago };
            return await _operacionRepository.EjecutarAsync(clienteId,
                () => CrearCoreAsync(clienteId, usuarioId, detalles, pago));
        }

        private static List<DetalleVentaDto> NormalizarDetalles(List<DetalleVentaDto>? detalles)
        {
            if (detalles == null || detalles.Count == 0)
                throw new ReglaFinancieraException("La venta debe contener al menos un detalle.", "venta_sin_detalles", 400);
            foreach (var detalle in detalles)
            {
                if (detalle == null || detalle.ProductoId.HasValue == detalle.VehiculoId.HasValue)
                    throw new ReglaFinancieraException("Cada detalle debe indicar solo un producto o un vehículo.", "detalle_invalido", 400);
                if ((detalle.ProductoId ?? detalle.VehiculoId) <= 0 || detalle.Cantidad <= 0)
                    throw new ReglaFinancieraException("Los identificadores y cantidades deben ser positivos.", "cantidad_o_id_invalido", 400);
                if (detalle.VehiculoId.HasValue && detalle.Cantidad != 1)
                    throw new ReglaFinancieraException("La cantidad de un vehículo debe ser uno.", "cantidad_vehiculo_invalida", 400);
            }
            var vehiculos = detalles.Count(d => d.VehiculoId.HasValue);
            if (vehiculos > 0)
            {
                if (vehiculos != detalles.Count)
                    throw new ReglaFinancieraException("No se pueden mezclar vehículos y artículos.", "venta_mixta", 400);
                if (vehiculos != 1)
                    throw new ReglaFinancieraException("La venta admite un único vehículo sin repetir.", "multiples_vehiculos", 400);
                return [new DetalleVentaDto { VehiculoId = detalles[0].VehiculoId, Cantidad = 1 }];
            }

            var cantidades = new Dictionary<int, int>();
            try
            {
                foreach (var detalle in detalles)
                {
                    var id = detalle.ProductoId!.Value;
                    cantidades.TryGetValue(id, out var anterior);
                    cantidades[id] = checked(anterior + detalle.Cantidad);
                }
            }
            catch (OverflowException)
            { throw new ReglaFinancieraException("La cantidad acumulada excede el rango admitido.", "cantidad_fuera_de_rango", 400); }

            // Global acquisition order: client (Fase 1), then product keys ascending.
            return cantidades.OrderBy(item => item.Key).Select(item => new DetalleVentaDto
            { ProductoId = item.Key, Cantidad = item.Value }).ToList();
        }

        private async Task<VentaDto> CrearCoreAsync(int clienteId, int usuarioId,
            List<DetalleVentaDto> detalles, PagoInicialVentaDto? pagoInicial)
        {
            if (await _clienteRepository.ObtenerPorIdAsync(clienteId) == null)
                throw new ReglaFinancieraException("El cliente no existe.", "cliente_inexistente", 404);
            if (await _usuarioRepository.ObtenerPorIdAsync(usuarioId) == null)
                throw new ReglaFinancieraException("El usuario no existe.", "usuario_inexistente", 404);

            decimal subtotal = 0;
            var detallesVenta = new List<DetalleVenta>();
            foreach (var detalle in detalles)
            {
                decimal precio;
                if (detalle.ProductoId.HasValue)
                {
                    var producto = await _productoRepository.ObtenerParaVentaAsync(detalle.ProductoId.Value)
                        ?? throw new ReglaFinancieraException("Producto inexistente.", "producto_inexistente", 404);
                    if (producto.Stock < detalle.Cantidad)
                        throw new ReglaFinancieraException($"Stock insuficiente para {producto.Nombre}.", "stock_insuficiente");
                    precio = producto.PrecioVenta;
                }
                else
                {
                    var vehiculo = await _vehiculoRepository.ObtenerParaVentaAsync(detalle.VehiculoId!.Value)
                        ?? throw new ReglaFinancieraException("Vehículo inexistente.", "vehiculo_inexistente", 404);
                    if (vehiculo.Estado != "Disponible")
                        throw new ReglaFinancieraException("El vehículo ya no está disponible.", "vehiculo_no_disponible");
                    precio = vehiculo.PrecioVenta;
                }
                if (precio <= 0 || precio > 9999999999999999.99m || decimal.Round(precio, 2) != precio)
                    throw new ReglaFinancieraException("El ítem no tiene un precio de venta válido.", "precio_invalido");
                subtotal += precio * detalle.Cantidad;
                if (subtotal > 9999999999999999.99m)
                    throw new ReglaFinancieraException("El total excede el rango monetario admitido.", "total_fuera_de_rango", 400);
                detallesVenta.Add(new DetalleVenta
                { ProductoId = detalle.ProductoId, VehiculoId = detalle.VehiculoId, Cantidad = detalle.Cantidad, PrecioUnitario = precio });
            }

            var montoPago = pagoInicial?.Monto ?? 0;
            if (montoPago < 0 || montoPago > subtotal || decimal.Round(montoPago, 2) != montoPago)
                throw new ReglaFinancieraException("El pago inicial debe ser válido y no superar el total.", "pago_inicial_invalido", 400);
            if (montoPago > 0 && string.IsNullOrWhiteSpace(pagoInicial!.FormaPago))
                throw new ReglaFinancieraException("Debe indicar la forma de pago.", "forma_pago_requerida", 400);

            var estado = montoPago == 0 ? "Pendiente" : montoPago == subtotal ? "Pagada" : "PagoParcial";
            var fecha = DateTime.Now;
            var venta = new Venta
            {
                ClienteId = clienteId, UsuarioId = usuarioId, Fecha = fecha,
                Subtotal = subtotal, Total = subtotal, Estado = estado, DetallesVenta = detallesVenta
            };
            if (montoPago > 0)
                venta.Pagos.Add(new PagoVenta { Monto = montoPago, FormaPago = pagoInicial!.FormaPago, Fecha = fecha });

            // All validation and resource acquisition completed before the first write.
            await _ventaRepository.CrearAsync(venta);
            foreach (var detalle in detalles)
            {
                if (detalle.ProductoId.HasValue)
                    await _productoRepository.DescontarStockAsync(detalle.ProductoId.Value, detalle.Cantidad);
                else
                    await _vehiculoRepository.MarcarVendidoAsync(detalle.VehiculoId!.Value);
            }
            await _cuentaCorrienteRepository.CrearAsync(new CuentaCorriente
            { ClienteId = clienteId, Fecha = fecha, Concepto = $"Venta #{venta.Id}", Debe = subtotal, Haber = 0 });
            if (montoPago > 0)
                await _cuentaCorrienteRepository.CrearAsync(new CuentaCorriente
                { ClienteId = clienteId, Fecha = fecha, Concepto = $"Pago inicial Venta #{venta.Id}", Debe = 0, Haber = montoPago });

            return new VentaDto
            {
                Id = venta.Id, ClienteId = clienteId, UsuarioId = usuarioId, Fecha = fecha,
                Subtotal = subtotal, Total = subtotal, Estado = estado,
                TieneVehiculo = detalles[0].VehiculoId.HasValue, EstaPagada = estado == "Pagada", SaldoPendiente = subtotal - montoPago
            };
        }

        public async Task<VentaDto?> ObtenerPorIdAsync(int id)
        {
            var venta = await _ventaRepository.ObtenerPorIdAsync(id);

            if (venta == null)
                return null;

            var detalleVehiculo = venta.DetallesVenta
                .FirstOrDefault(d => d.VehiculoId.HasValue);

            var totalPagado = venta.Pagos.Sum(p => p.Monto);

            var saldoPendiente = venta.Total - totalPagado;


            return new VentaDto
            {
                Id = venta.Id,

                ClienteId = venta.ClienteId,
                NombreCliente = venta.Cliente.Nombre,

                UsuarioId = venta.UsuarioId,
                NombreUsuario = venta.Usuario.Nombre,

                Fecha = venta.Fecha,

                Subtotal = venta.Subtotal,
                Total = venta.Total,

                Estado = venta.Estado,

                VehiculoId = detalleVehiculo?.VehiculoId,

                NombreVehiculo = detalleVehiculo != null
                    ? $"{detalleVehiculo.Vehiculo.Modelo.MarcaVehiculo.Nombre} {detalleVehiculo.Vehiculo.Modelo.Nombre}"
                    : string.Empty,

                PrecioVehiculo = detalleVehiculo?.PrecioUnitario ?? 0,

                TieneVehiculo = venta.DetallesVenta
                    .Any(d => d.VehiculoId != null),

                TieneFinanciacion = venta.Financiacion != null,

                EstaPagada = venta.Estado == "Pagada",

                SaldoPendiente = saldoPendiente,

                Detalles = venta.DetallesVenta
                    .Select(d => new DetalleVentaResponseDto
                    {
                        Id = d.Id,

                        ProductoId = d.ProductoId,

                        NombreProducto = d.Producto != null
                            ? d.Producto.Nombre
                            : null,

                        VehiculoId = d.VehiculoId,

                        NombreVehiculo = d.Vehiculo != null
                            ? $"{d.Vehiculo.Modelo.MarcaVehiculo.Nombre} {d.Vehiculo.Modelo.Nombre}"
                            : null,

                        Cantidad = d.Cantidad,

                        PrecioUnitario = d.PrecioUnitario,

                        Subtotal = d.PrecioUnitario * d.Cantidad

                    })
                    .ToList(),
                        Pagos = venta.Pagos
                        .Select(p => new PagoVentaDto
                        {
                            Id = p.Id,
                            VentaId = p.VentaId,
                            FormaPago = p.FormaPago,
                            Monto = p.Monto,
                            Fecha = p.Fecha
                        })
                        .ToList(),
                    FinanciacionId = venta.Financiacion?.Id,
                Financiacion = venta.Financiacion == null
    ? null
    : new FinanciacionDto
    {
        Id = venta.Financiacion.Id,

        VentaId = venta.Financiacion.VentaId,

        CantidadCuotas = venta.Financiacion.CantidadCuotas,

        MontoFinanciado =
            venta.Financiacion.MontoFinanciado,

        CoeficienteAplicado =
            venta.Financiacion.CoeficienteAplicado,

        MontoTotal =
            venta.Financiacion.MontoTotal,

        InteresTotal =
            venta.Financiacion.MontoTotal
            - venta.Financiacion.MontoFinanciado,

        MontoPagado =
            venta.Financiacion.Cuotas
                .Where(c => c.Estado == "Pagada")
                .Sum(c => c.Monto),

        SaldoPendiente =
            venta.Financiacion.MontoTotal
            - venta.Financiacion.Cuotas
                .Where(c => c.Estado == "Pagada")
                .Sum(c => c.Monto),

        Estado = venta.Financiacion.Estado,

        FechaCreacion =
            venta.Financiacion.FechaCreacion
    },
            };
        }

        public async Task<List<VentaDto>> ObtenerTodosAsync()
        {
            var ventas = await _ventaRepository.ObtenerTodosAsync();

            return ventas.Select(v =>
            {
                var totalPagado = v.Pagos.Sum(p => p.Monto);

                return new VentaDto
                {
                    Id = v.Id,
                    ClienteId = v.ClienteId,
                    NombreCliente = v.Cliente.Nombre,
                    UsuarioId = v.UsuarioId,
                    NombreUsuario = v.Usuario.Nombre,
                    Fecha = v.Fecha,
                    Subtotal = v.Subtotal,
                    Total = v.Total,
                    Estado = v.Estado,

                    TieneVehiculo = v.DetallesVenta.Any(d => d.VehiculoId != null),
                    TieneFinanciacion = v.Financiacion != null,
                    FinanciacionId = v.Financiacion?.Id,
                    EstaPagada = v.Estado == "Pagada",

                    SaldoPendiente = v.Total - totalPagado
                };
            }).ToList();
        }
        public async Task<VentaResumenDto> ObtenerResumenAsync()
        {
            return await _ventaRepository
                .ObtenerResumenAsync();
        }
    }
}

