using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaDeVentas.Controllers;
using SistemaDeVentas.Repositories;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services;
using SistemaDeVentas.Services.Interfaces;

namespace OptiGest.Tests.Sql;

[Collection("TestingSql"), Trait("Suite", "Sql")]
public class FinancialHttpTests(TestingDatabase database)
{
    internal static async Task<TestServer> CreateServer(TestingDatabase database)
    {
        await database.VerifyOwnershipAsync();
        return new TestServer(new WebHostBuilder().UseEnvironment("Testing")
            .ConfigureAppConfiguration((_, builder) => builder.Sources.Clear())
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddControllers().AddApplicationPart(typeof(FinanciacionController).Assembly);
                services.AddAuthentication("Testing").AddScheme<AuthenticationSchemeOptions, TestingAuthentication>("Testing", _ => { });
                services.AddAuthorization();
                services.AddScoped(_ => database.CreateContextForHost());
                services.AddScoped<IOperacionFinancieraRepository, OperacionFinancieraRepository>();
                services.AddScoped<IVentaRepository, VentaRepository>();
                services.AddScoped<IFinanciacionRepository, FinanciacionRepository>();
                services.AddScoped<IConfiguracionFinanciacionRepository, ConfiguracionFinanciacionRepository>();
                services.AddScoped<ICuotaRepository, CuotaRepository>();
                services.AddScoped<ICuentaCorrienteRepository, CuentaCorrienteRepository>();
                services.AddScoped<IPagoVentaRepository, PagoVentaRepository>();
                services.AddScoped<IPagoCuotaRepository, PagoCuotaRepository>();
                services.AddScoped<IClienteRepository, ClienteRepository>();
                services.AddScoped<IUsuarioRepository, UsuarioRepository>();
                services.AddScoped<IProductoRepository, ProductoRepository>();
                services.AddScoped<IVehiculoRepository, VehiculoRepository>();
                services.AddScoped<IVentaService, VentaService>();
                services.AddScoped<IFinanciacionService, FinanciacionService>();
                services.AddScoped<IPagoVentaService, PagoVentaService>();
                services.AddScoped<IPagoCuotaService, PagoCuotaService>();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            }));
    }

    [SqlTheory]
    [InlineData(0, HttpStatusCode.BadRequest, "identificador_invalido")]
    [InlineData(int.MaxValue, HttpStatusCode.NotFound, "venta_inexistente")]
    public async Task Financial_errors_are_actual_controlled_http_responses(int saleId, HttpStatusCode status, string code)
    {
        using var server = await CreateServer(database);
        using var client = server.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/Financiacion", new { ventaId = saleId, configuracionFinanciacionId = 1 });
        Assert.Equal(status, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, payload.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(payload.GetProperty("message").GetString()));
    }

    [SqlFact]
    public async Task Successful_http_response_has_committed_plan_and_later_conflicts_are_409()
    {
        await using var setup = await database.CreateContextAsync();
        var seed = await SyntheticCase.SeedAsync(setup);
        using var server = await CreateServer(database);
        using var client = server.CreateClient();
        var request = new { ventaId = seed.SaleId, configuracionFinanciacionId = seed.PlanId };
        using var created = await client.PostAsJsonAsync("/api/Financiacion", request);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var payload = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = payload.GetProperty("id").GetInt32();
        await using var reader = await database.CreateContextAsync();
        Assert.Equal(3, await reader.Cuotas.CountAsync(c => c.FinanciacionId == id));
        using var duplicate = await client.PostAsJsonAsync("/api/Financiacion", request);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("financiacion_existente", (await duplicate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        using var direct = await client.PostAsJsonAsync("/api/PagoVenta", new { ventaId = seed.SaleId, monto = 10m, formaPago = "Efectivo" });
        Assert.Equal(HttpStatusCode.Conflict, direct.StatusCode);
        Assert.Equal("venta_financiada", (await direct.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    // Test-only authentication: no JWT keys, UserSecrets, production Program or listening port.
    public sealed class TestingAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Role, "Admin") }, "Testing");
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), "Testing")));
        }
    }
}
