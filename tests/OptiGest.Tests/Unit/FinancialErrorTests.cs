using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using SistemaDeVentas.Helpers;

namespace OptiGest.Tests.Unit;

[Trait("Suite", "Unit")]
public class FinancialErrorTests
{
    [Theory]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(503)]
    public void Business_errors_keep_compatible_message_and_controlled_status(int status)
    {
        var action = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var context = new ExceptionContext(action, []) { Exception = new ReglaFinancieraException("Synthetic rule", "synthetic_code", status) };
        new FinancialExceptionFilter(NullLogger<FinancialExceptionFilter>.Instance).OnException(context);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(status, result.StatusCode);
        var json = JsonSerializer.SerializeToElement(result.Value);
        Assert.Equal("Synthetic rule", json.GetProperty("message").GetString());
        Assert.Equal("synthetic_code", json.GetProperty("code").GetString());
        Assert.True(context.ExceptionHandled);
    }

    [Fact]
    public void Unexpected_database_details_are_not_returned_to_client()
    {
        var action = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var context = new ExceptionContext(action, []) { Exception = new Exception("Synthetic private database detail") };
        new FinancialExceptionFilter(NullLogger<FinancialExceptionFilter>.Instance).OnException(context);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(500, result.StatusCode);
        Assert.DoesNotContain("private database", JsonSerializer.Serialize(result.Value));
    }
}
