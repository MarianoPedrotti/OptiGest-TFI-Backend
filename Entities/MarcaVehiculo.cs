namespace SistemaDeVentas.Entities
{
    public class MarcaVehiculo
    {
        public int Id { get; set; }

        public string Nombre { get; set; }
        public ICollection<Modelo> Modelos { get; set; } = new List<Modelo>();
    }
}
