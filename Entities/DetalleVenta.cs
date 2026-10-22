using SistemaDeVentas.Entities;

public class DetalleVenta
{
    public int Id { get; set; }

    public int VentaId { get; set; }

    public int? ProductoId { get; set; }

    public int? VehiculoId { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    // Navegación

    public Venta Venta { get; set; }

    public Producto? Producto { get; set; }

    public Vehiculo? Vehiculo { get; set; }
}