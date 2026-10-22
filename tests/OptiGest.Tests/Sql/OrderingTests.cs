using Microsoft.EntityFrameworkCore;
using SistemaDeVentas.DTOs.Financiaciones;

namespace OptiGest.Tests.Sql;

[Collection("TestingSql"), Trait("Suite", "Sql")]
public class OrderingTests(TestingDatabase database)
{
    [SqlFact]
    public async Task Required_sales_api_source_is_newest_first_with_id_tiebreaker()
    {
        await using var db = await database.CreateContextAsync();
        var old = await SyntheticCase.SeedAsync(db);
        var tiedLowerId = await SyntheticCase.SeedAsync(db);
        var tiedHigherId = await SyntheticCase.SeedAsync(db);
        // Equal dates require deterministic descending Id.
        var sales = await db.Ventas
            .Where(v => v.Id == old.SaleId || v.Id == tiedLowerId.SaleId || v.Id == tiedHigherId.SaleId)
            .ToListAsync();
        sales.Single(v => v.Id == old.SaleId).Fecha = new DateTime(2025, 12, 31);
        sales.Where(v => v.Id == tiedLowerId.SaleId || v.Id == tiedHigherId.SaleId)
            .ToList()
            .ForEach(v => v.Fecha = new DateTime(2026, 1, 1));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var response = await TestingDatabase.Sales(db).ObtenerTodosAsync();
        Assert.Equal(
            new[] { tiedHigherId.SaleId, tiedLowerId.SaleId, old.SaleId },
            response.Where(v => v.Id == old.SaleId || v.Id == tiedLowerId.SaleId || v.Id == tiedHigherId.SaleId).Select(v => v.Id));
    }

    [SqlFact]
    public async Task Required_financing_api_source_is_newest_first_with_id_tiebreaker()
    {
        await using var db = await database.CreateContextAsync();
        var oldest = await SyntheticCase.SeedAsync(db);
        var tiedLowerId = await SyntheticCase.SeedAsync(db);
        var tiedHigherId = await SyntheticCase.SeedAsync(db);
        db.ChangeTracker.Clear();
        var service = TestingDatabase.Financing(db);
        var a = await service.CrearAsync(new CrearFinanciacionDto { VentaId = oldest.SaleId, ConfiguracionFinanciacionId = oldest.PlanId });
        var b = await service.CrearAsync(new CrearFinanciacionDto { VentaId = tiedLowerId.SaleId, ConfiguracionFinanciacionId = tiedLowerId.PlanId });
        var c = await service.CrearAsync(new CrearFinanciacionDto { VentaId = tiedHigherId.SaleId, ConfiguracionFinanciacionId = tiedHigherId.PlanId });
        var financings = await db.Financiaciones.Where(f => f.Id == a.Id || f.Id == b.Id || f.Id == c.Id).ToListAsync();
        financings.Single(f => f.Id == a.Id).FechaCreacion = new DateTime(2025, 12, 31);
        financings.Where(f => f.Id == b.Id || f.Id == c.Id)
            .ToList()
            .ForEach(f => f.FechaCreacion = new DateTime(2026, 1, 1));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var response = await service.ObtenerTodosAsync();
        Assert.Equal(
            new[] { c.Id, b.Id, a.Id },
            response.Where(f => f.Id == a.Id || f.Id == b.Id || f.Id == c.Id).Select(f => f.Id));
    }

    [SqlFact]
    public async Task Sales_list_includes_the_financing_id_for_a_financed_sale()
    {
        await using var db = await database.CreateContextAsync();
        var seed = await SyntheticCase.SeedAsync(db);
        var financing = await TestingDatabase.Financing(db).CrearAsync(
            new CrearFinanciacionDto
            {
                VentaId = seed.SaleId,
                ConfiguracionFinanciacionId = seed.PlanId
            });

        db.ChangeTracker.Clear();
        var sales = await TestingDatabase.Sales(db).ObtenerTodosAsync();

        Assert.Equal(financing.Id, sales.Single(sale => sale.Id == seed.SaleId).FinanciacionId);
    }
}
