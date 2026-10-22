namespace SistemaDeVentas.DTOs.Modelos
{
    public class ModeloCreateDto
    {
        public int MarcaVehiculoId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public short Cilindrada { get; set; }

        public string TipoMotor { get; set; } = string.Empty;
    }
}