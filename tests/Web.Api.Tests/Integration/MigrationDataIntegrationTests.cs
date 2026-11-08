using Domain.Products;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Web.Api.Tests.TestSupport;

namespace Web.Api.Tests.Integration;

/// <summary>Checks that migrations which reshape data carry existing rows over correctly.</summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public class MigrationDataIntegrationTests(PostgresApiFixture api)
{
    [Fact]
    public async Task RecordUnitCostOnPurchaseEntries_CopiesTheLinkedPriceIntoUnitCost()
    {
        using var host = new ApiHost("Production", api.ConnectionStringFor($"migration_{Guid.NewGuid():N}"));
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var migrator = context.GetService<IMigrator>();
        var entryId = Guid.NewGuid();

        await migrator.MigrateAsync("20261107160317_AddStockAdjustment");
        await context.Database.ExecuteSqlRawAsync(SeedBeforeUnitCostSql(entryId));

        await migrator.MigrateAsync();

        var query = $"SELECT unit_cost AS \"Value\" FROM public.purchase_product_entry WHERE id = '{entryId}'";
        var unitCost = await context.Database.SqlQueryRaw<decimal>(query).SingleAsync();
        Assert.Equal(12.5m, unitCost);
    }

    [Fact]
    public async Task AddStockDefinitionToProduct_GivesExistingProductsTheDefaultMinimum()
    {
        using var host = new ApiHost("Production", api.ConnectionStringFor($"migration_{Guid.NewGuid():N}"));
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var migrator = context.GetService<IMigrator>();
        var entryId = Guid.NewGuid();

        await migrator.MigrateAsync("20261107160317_AddStockAdjustment");
        await context.Database.ExecuteSqlRawAsync(SeedBeforeUnitCostSql(entryId));

        await migrator.MigrateAsync();

        var minimum = await context.Database
            .SqlQueryRaw<int>("SELECT stock_minimum_quantity AS \"Value\" FROM public.product")
            .SingleAsync();
        Assert.Equal(StockDefinition.DefaultMinimumQuantity, minimum);
    }

    private static string SeedBeforeUnitCostSql(Guid entryId)
    {
        var productId = Guid.NewGuid();
        var priceId = Guid.NewGuid();
        var purchaseId = Guid.NewGuid();

        return $"""
            INSERT INTO public.brands (id, name, created_time) VALUES (1, 'Brand', now());
            INSERT INTO public.product_unit (id, name, normalized_name) VALUES (1, 'Unit', 'unit');
            INSERT INTO public.product (id, name, created_time, last_updated_time)
                VALUES ('{productId}', 'Milk', now(), now());
            INSERT INTO public.product_detail (product_id, description, brand_id, measure, measure_unit_id)
                VALUES ('{productId}', NULL, 1, 1, 1);
            INSERT INTO public.product_price (id, product_id, value, created_time, valid_from_time, valid_to_time)
                VALUES ('{priceId}', '{productId}', 12.5, now(), now(), NULL);
            INSERT INTO public.purchase (id, title, occurence_time, created_time, last_updated_time)
                VALUES ('{purchaseId}', 'Restock', now(), now(), now());
            INSERT INTO public.purchase_product_entry (id, product_id, price_id, quantity, purchase_id)
                VALUES ('{entryId}', '{productId}', '{priceId}', 3, '{purchaseId}');
            """;
    }
}
