using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SistemaDeVentas.Entities;

namespace OptiGest.Tests.Sql;

internal sealed class InjectedFailure : Exception;

// Test-only failures BEFORE the matching write; all earlier writes really hit SQL.
internal sealed class FailOnWrite(Func<DbContext, bool> shouldFail) : SaveChangesInterceptor
{
    public bool Triggered { get; private set; }
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context && shouldFail(context))
        { Triggered = true; throw new InjectedFailure(); }
        return ValueTask.FromResult(result);
    }

    public static bool SecondInstallment(DbContext db) => db.ChangeTracker.Entries<Cuota>()
        .Any(e => e.State == EntityState.Added && e.Entity.Numero == 2);
    public static bool LedgerCredit(DbContext db) => db.ChangeTracker.Entries<CuentaCorriente>()
        .Any(e => e.State == EntityState.Added && e.Entity.Haber > 0);
}
