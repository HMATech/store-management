using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
