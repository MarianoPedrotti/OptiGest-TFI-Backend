using Moq;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services;

namespace OptiGest.Tests.Unit;

[Trait("Suite", "Unit")]
public class CurrentAccountTests
{
    [Theory]
    [InlineData(30, 70)]
    [InlineData(100, 0)]
    [InlineData(130, -30)]
    public async Task Current_balance_uses_all_amounts_not_the_last_historical_balance(int credit, int expected)
    {
        // Date presentation order differs from insertion order; snapshots are preserved.
        var movements = new List<CuentaCorriente>
        {
            new() { Id = 2, ClienteId = 1, Fecha = new DateTime(2026, 10, 19), Haber = credit, Saldo = 100 - credit },
            new() { Id = 1, ClienteId = 1, Fecha = new DateTime(2026, 10, 20), Debe = 100, Saldo = 100 }
        };
        var repository = new Mock<ICuentaCorrienteRepository>(MockBehavior.Strict);
        repository.Setup(r => r.ObtenerPorClienteAsync(1)).ReturnsAsync(movements);

        var result = await new CuentaCorrienteService(repository.Object).ObtenerCuentaClienteAsync(1);

        Assert.Equal(expected, result.SaldoActual);
        Assert.Equal(new[] { 2, 1 }, result.Movimientos.Select(m => m.Id));
        Assert.Equal(new[] { 100m - credit, 100m }, result.Movimientos.Select(m => m.Saldo));
        repository.Verify(r => r.ObtenerPorClienteAsync(1), Times.Once);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Account_without_movements_has_zero_balance()
    {
        var repository = new Mock<ICuentaCorrienteRepository>();
        repository.Setup(r => r.ObtenerPorClienteAsync(1)).ReturnsAsync(new List<CuentaCorriente>());
        var result = await new CuentaCorrienteService(repository.Object).ObtenerCuentaClienteAsync(1);
        Assert.Equal(0m, result.SaldoActual);
        Assert.Empty(result.Movimientos);
    }
}
