namespace SistemaDeVentas.DTOs.Vehiculos
{
    public class VehiculoResumenDto
    {
        public int TotalVehiculos { get; set; }

        public int Disponibles { get; set; }

        public int Vendidos { get; set; }

        public decimal ValorDisponibles { get; set; }
    }
}