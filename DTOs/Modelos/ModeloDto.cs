namespace SistemaDeVentas.DTOs.Modelos
{
    public class ModeloDto
    {
        public int Id { get; set; }

        public string nombreMarca { get; set; } = string.Empty;

        public int MarcaVehiculoId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public short Cilindrada { get; set; }

        public string TipoMotor { get; set; } = string.Empty;
    }
}