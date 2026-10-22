using Moq;
using SistemaDeVentas.DTOs.Financiaciones;
using SistemaDeVentas.Entities;

namespace OptiGest.Tests.Unit;

[Trait("Suite", "Unit")]
public class FinancingTests
{
    private static CrearFinanciacionDto Request() => new() { VentaId = 40, ConfiguracionFinanciacionId = 30 };

    [Theory]
    [InlineData("Pagada")]
    [InlineData("ExistingFinancing")]
    [InlineData("ZeroBalance")]
    [InlineData("InactivePlan")]
    [InlineData("MissingSale")]
    [InlineData("MissingPlan")]
    public async Task Current_rejections_happen_before_writes(string scenario)
    {
        var h = new BusinessHarness();
        switch (scenario)
        {
            case "Pagada": h.Sale.Estado = "Pagada"; break;
            case "ExistingFinancing": h.Sale.Financiacion = new Financiacion(); break;
            case "ZeroBalance": h.Sale.Pagos.Add(new PagoVenta { Monto = 1000m }); break;
            case "InactivePlan": h.Plan.Activa = false; break;
            case "MissingSale": h.Sales.Setup(r => r.ObtenerPorIdAsync(40)).ReturnsAsync((Venta?)null); break;
            case "MissingPlan": h.Plans.Setup(r => r.ObtenerPorIdAsync(30)).ReturnsAsync((ConfiguracionFinanciacion?)null); break;
        }
        await Assert.ThrowsAnyAsync<Exception>(() => h.FinancingService().CrearAsync(Request()));
        Assert.Empty(h.CreatedFinancing);
        Assert.Empty(h.CreatedInstallments);
        h.Ledger.Verify(r => r.CrearAsync(It.IsAny<CuentaCorriente>()), Times.Never);
    }

    [Fact]
    public async Task Principal_uses_payments_and_installments_sum_to_total()
    {
        var h = new BusinessHarness();
        h.Sale.Pagos.Add(new PagoVenta { Monto = 333.33m });
        var result = await h.FinancingService().CrearAsync(Request());
        Assert.Equal(666.67m, result.MontoFinanciado);
        Assert.Equal(733.34m, result.MontoTotal);
        Assert.Equal(3, h.CreatedInstallments.Count);
        Assert.Equal(result.MontoTotal, h.CreatedInstallments.Sum(c => c.Monto));
        Assert.Equal(new[] { 1, 2, 3 }, h.CreatedInstallments.Select(c => c.Numero));
    }

    [Theory]
    [InlineData(1000, 2000)]
    [InlineData(0, 1000)]
    public async Task Current_plan_boundaries_are_inclusive(int min, int max)
    {
        var h = new BusinessHarness();
        h.Plan.MontoMinimo = min;
        h.Plan.MontoMaximo = max;
        await h.FinancingService().CrearAsync(Request());
        Assert.Single(h.CreatedFinancing);
    }

    [Fact]
    public async Task New_rule_rejects_article_only_sale()
    {
        var h = new BusinessHarness();
        h.Sale.DetallesVenta.Clear();
        h.Sale.DetallesVenta.Add(new DetalleVenta { ProductoId = 10, Cantidad = 1 });
        await Assert.ThrowsAnyAsync<Exception>(() => h.FinancingService().CrearAsync(Request()));
        Assert.Empty(h.CreatedFinancing);
    }

    [Fact]
    public async Task New_rule_accepts_plan_by_remaining_balance_not_original_total()
    {
        var h = new BusinessHarness();
        h.Sale.Total = 5_000_000m;
        h.Sale.Pagos.Add(new PagoVenta { Monto = 2_000_000m });
        h.Plan.MontoMinimo = 3_000_000m;
        h.Plan.MontoMaximo = 3_000_000m;
        var result = await h.FinancingService().CrearAsync(Request());
        Assert.Equal(3_000_000m, result.MontoFinanciado);
    }

    [Theory]
    [InlineData(0, 10, 1000)]
    [InlineData(3, -1, 1000)]
    [InlineData(3, 0, 0.01)]
    [InlineData(3, 0, 0.02)]
    public async Task Invalid_or_unpayable_plan_is_rejected_before_persisting(int count, int rate, double total)
    {
        var h = new BusinessHarness();
        h.Sale.Total = (decimal)total;
        h.Plan.CantidadCuotas = count;
        h.Plan.TasaInteres = rate;
        await Assert.ThrowsAnyAsync<Exception>(() => h.FinancingService().CrearAsync(Request()));
        Assert.Empty(h.CreatedFinancing);
        Assert.Empty(h.CreatedInstallments);
    }

    [Theory]
    [InlineData(600, 1000, true)]
    [InlineData(0, 600, true)]
    [InlineData(601, 1000, false)]
    [InlineData(0, 599, false)]
    public async Task Plan_limits_apply_inclusively_to_balance_after_payments(int min, int max, bool allowed)
    {
        var h = new BusinessHarness();
        h.Sale.Pagos.Add(new PagoVenta { Monto = 400m });
        h.Plan.MontoMinimo = min;
        h.Plan.MontoMaximo = max;
        if (allowed) Assert.Equal(600m, (await h.FinancingService().CrearAsync(Request())).MontoFinanciado);
        else
        {
            await Assert.ThrowsAnyAsync<Exception>(() => h.FinancingService().CrearAsync(Request()));
            Assert.Empty(h.CreatedFinancing);
        }
    }

    [Fact]
    public async Task Unrepresentable_due_date_is_rejected_before_persisting()
    {
        var h = new BusinessHarness();
        h.Sale.Total = 1000000m;
        h.Plan.CantidadCuotas = 100000;
        await Assert.ThrowsAnyAsync<Exception>(() => h.FinancingService().CrearAsync(Request()));
        Assert.Empty(h.CreatedFinancing);
        Assert.Empty(h.CreatedInstallments);
    }
}
