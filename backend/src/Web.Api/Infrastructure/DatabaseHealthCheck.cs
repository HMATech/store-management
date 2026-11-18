using Infrastructure.Database;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Web.Api.Infrastructure;

/// <summary>Healthy when the API can reach its database, which it needs for nearly every request.</summary>
internal sealed class DatabaseHealthCheck(ApplicationDbContext dbContext) : IHealthCheck
{
    public const string Name = "database";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The database can't be reached");
    }
}
