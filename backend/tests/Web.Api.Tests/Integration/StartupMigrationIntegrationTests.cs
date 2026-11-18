using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Web.Api.Infrastructure;
using Web.Api.Tests.TestSupport;

namespace Web.Api.Tests.Integration;

[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public class StartupMigrationIntegrationTests(PostgresApiFixture api)
{
    [Fact]
    public async Task Startup_InProductionWithApplyMigrationsOnStartup_AppliesMigrations()
    {
        using var host = CreateProductionHost(applyMigrations: "true");

        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public async Task Startup_InProductionWithoutApplyMigrationsOnStartup_LeavesDatabaseUntouched(
        string? applyMigrations)
    {
        using var host = CreateProductionHost(applyMigrations);

        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Only migrating creates the database, so it still doesn't exist.
        Assert.False(await context.Database.CanConnectAsync());
    }

    [Fact]
    public async Task Startup_WhenAMigrationFails_FailsInsteadOfServingAnOutdatedDatabase()
    {
        var database = $"startup_{Guid.NewGuid():N}";
        await using (var server = new NpgsqlConnection(api.ConnectionStringFor()))
        {
            await server.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE {database}", server);
            await create.ExecuteNonQueryAsync();
        }
        // A table the first migration creates already exists, so that migration fails.
        await using (var target = new NpgsqlConnection(api.ConnectionStringFor(database)))
        {
            await target.OpenAsync();
            await using var conflict = new NpgsqlCommand("CREATE TABLE public.brands (id integer)", target);
            await conflict.ExecuteNonQueryAsync();
        }

        using var host = new ApiHost("Production", api.ConnectionStringFor(database),
            new Dictionary<string, string?> { [MigrationExtensions.ApplyOnStartupKey] = "true" });

        Assert.ThrowsAny<Exception>(() => host.Services);
    }

    private ApiHost CreateProductionHost(string? applyMigrations)
    {
        var settings = new Dictionary<string, string?>();
        if (applyMigrations != null)
        {
            settings[MigrationExtensions.ApplyOnStartupKey] = applyMigrations;
        }

        var host = new ApiHost("Production", api.ConnectionStringFor($"startup_{Guid.NewGuid():N}"), settings);
        _ = host.Services;
        return host;
    }
}
