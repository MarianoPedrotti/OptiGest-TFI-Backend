using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SistemaDeVentas.DTOs.PagosVenta;
using SistemaDeVentas.Entities;

namespace OptiGest.Tests.Sql;

[Collection("TestingSql"), Trait("Suite", "Sql")]
public class ConcurrencyTests(TestingDatabase database)
{
    private sealed class FirstPaymentGate(bool requireBoth) : SaveChangesInterceptor
    {
        private int _arrived;
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<PagoVenta>().Any(e => e.State == EntityState.Added))
            {
                if (Interlocked.Increment(ref _arrived) == 2) _ready.TrySetResult();
                // Once business locking is implemented the second request may be
                // blocked before this checkpoint. Release the first instead of
                // turning correct serialization into a test deadlock.
                try { await _ready.Task.WaitAsync(TimeSpan.FromSeconds(requireBoth ? 15 : 2), cancellationToken); }
                catch (TimeoutException) when (!requireBoth) { _ready.TrySetResult(); }
            }
            return result;
        }
    }

    private async Task<(int Successful, int Payments, decimal Paid)> RacePayments(bool characterize = false)
    {
        await using var seedDb = await database.CreateContextAsync();
        var seed = await SyntheticCase.SeedAsync(seedDb);
        var gate = new FirstPaymentGate(characterize);
        await using var first = await database.CreateContextAsync(gate);
        await using var second = await database.CreateContextAsync(gate);
        async Task<bool> Pay(SistemaDeVentas.Data.AppDbContext context)
        {
            try
            {
                await TestingDatabase.Payments(context).CrearAsync(new PagoVentaCreateDto
                { VentaId = seed.SaleId, Monto = 800m, FormaPago = "Efectivo" });
                return true;
            }
            catch (InvalidOperationException)
            {
                // Only a controlled business rejection is acceptable. SQL failures,
                // cancellations and arbitrary exceptions still fail the test.
                return false;
            }
        }
        var results = await Task.WhenAll(Pay(first), Pay(second)).WaitAsync(TimeSpan.FromSeconds(30));
        await using var reader = await database.CreateContextAsync();
        return (results.Count(success => success), await reader.PagosVenta.CountAsync(p => p.VentaId == seed.SaleId),
            await reader.PagosVenta.Where(p => p.VentaId == seed.SaleId).SumAsync(p => p.Monto));
    }

    [SqlFact]
    public async Task Required_concurrent_payments_must_not_exceed_sale_total()
    {
        var state = await RacePayments();
        Assert.Equal(1, state.Successful);
        Assert.Equal(1, state.Payments);
        Assert.True(state.Paid <= 1000m, $"Persisted total paid: {state.Paid}");
    }
}
