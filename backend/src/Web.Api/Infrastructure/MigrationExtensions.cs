using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Web.Api.Infrastructure;

public static class MigrationExtensions
{
    /// <summary>Applies pending migrations on startup outside Development when set to true.</summary>
    public const string ApplyOnStartupKey = "Database:ApplyMigrationsOnStartup";

    public static IApplicationBuilder ApplyMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(MigrationExtensions));

        logger.LogInformation("Checking pending migrations...");
        var pendingMigrations = dbContext.Database.GetPendingMigrations();

        if (!pendingMigrations.Any())
        {
            logger.LogInformation("No pending migrations.");
            return app;
        }

        try
        {
            logger.LogInformation("Applying migrations...");
            dbContext.Database.Migrate();
            logger.LogInformation("Successfully applied migrations");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred while applying migrations");
        }

        return app;
    }
}