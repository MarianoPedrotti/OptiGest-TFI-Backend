namespace SistemaDeVentas.DTOs.ConfiguracionFinanciacion
{
    public class ActualizarConfiguracionFinanciacionDto
    {
        public int Id { get; set; }

        public int CantidadCuotas { get; set; }

        public decimal TasaInteres { get; set; }
        public decimal MontoMinimo { get; set; }

        public decimal MontoMaximo { get; set; }

        public bool Activa { get; set; }
    }
}
