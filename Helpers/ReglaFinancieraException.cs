namespace SistemaDeVentas.Helpers;

public class ReglaFinancieraException : InvalidOperationException
{
    public int EstadoHttp { get; }
    public string Codigo { get; }

    public ReglaFinancieraException(string mensaje, string codigo, int estadoHttp = 409, Exception? inner = null)
        : base(mensaje, inner)
    {
        Codigo = codigo;
        EstadoHttp = estadoHttp;
    }
}

public sealed class ResultadoFinancieroDesconocidoException : ReglaFinancieraException
{
    public ResultadoFinancieroDesconocidoException(Exception inner)
        : base("No se pudo confirmar el resultado de la operación. Consulte su estado antes de repetirla.",
            "resultado_no_confirmado", 503, inner) { }
}
