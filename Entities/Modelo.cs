namespace SistemaDeVentas.Entities
{
    public class Modelo
    {
        public int Id { get; set; }

        public int MarcaVehiculoId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public short Cilindrada { get; set; }

        public string TipoMotor { get; set; } = string.Empty;
        public MarcaVehiculo MarcaVehiculo { get; set; }
        public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();

    }
}
