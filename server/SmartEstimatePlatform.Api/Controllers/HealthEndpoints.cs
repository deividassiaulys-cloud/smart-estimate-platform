using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SmartEstimatePlatform.Api.Data;

namespace SmartEstimatePlatform.Api.Controllers;

/// <summary>
/// The health endpoints the deployment checks (feature 022): /health (liveness, with the deployed version) and
/// /health/ready (the database answers). Both are public, need no token and are not rate limited.
/// </summary>
public static class HealthEndpoints
{
    private const string ReadyTag = "ready";

    /// <summary>
    /// Registers the database check used by /health/ready.
    /// </summary>
    public static IServiceCollection AddHealthEndpoints(this IServiceCollection services)
    {
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: [ReadyTag]);
        return services;
    }

    /// <summary>
    /// Maps both endpoints. /health runs no check, so it answers even when the database is down; it reports the
    /// version from APP_VERSION (the commit the image was built from), or "dev" when none is set.
    /// /health/ready answers "Healthy" (200) or "Unhealthy" (503) and nothing more.
    /// </summary>
    public static void MapHealthEndpoints(this WebApplication app)
    {
        var version = app.Configuration["APP_VERSION"] is { Length: > 0 } configured ? configured : "dev";

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = (context, report) =>
                context.Response.WriteAsJsonAsync(new { status = report.Status.ToString(), version })
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) });
    }
}
