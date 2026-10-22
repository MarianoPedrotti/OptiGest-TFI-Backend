namespace SistemaDeVentas.DTOs.ConfiguracionFinanciacion
{
    public class CrearConfiguracionFinanciacionDto
    {
        public int CantidadCuotas { get; set; }

        public decimal TasaInteres { get; set; }
        public decimal MontoMinimo { get; set; }

        public decimal MontoMaximo { get; set; }
    }
}
