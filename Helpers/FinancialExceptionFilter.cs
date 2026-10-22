using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SistemaDeVentas.Helpers;

// Applied only to financial controllers; keeps the existing { message } contract.
public sealed class FinancialExceptionFilter(ILogger<FinancialExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is ReglaFinancieraException rule)
        {
            if (rule is ResultadoFinancieroDesconocidoException)
                logger.LogError(context.Exception, "Resultado financiero no confirmado");
            context.Result = new ObjectResult(new { message = rule.Message, code = rule.Codigo }) { StatusCode = rule.EstadoHttp };
        }
        else
        {
            logger.LogError(context.Exception, "Error inesperado en una operación financiera");
            context.Result = new ObjectResult(new
            { message = "No se pudo completar la operación financiera.", code = "error_financiero" }) { StatusCode = 500 };
        }
        context.ExceptionHandled = true;
    }
}
