using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SmartEstimatePlatform.Api.Data;

/// <summary>
/// Reports whether the database answers, for the readiness endpoint /health/ready (feature 022).
/// </summary>
public sealed class DatabaseHealthCheck(AppDbContext db) : IHealthCheck
{
    /// <summary>
    /// Healthy when a connection to the database can be opened; the reason for a failure is not reported.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy();
}
