using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;

namespace SmartEstimatePlatform.Api.Controllers;

/// <summary>
/// The rate limit on login and registration (feature 021, F4): a fixed window per client address, shared by both
/// endpoints. Configured in "RateLimiting:Auth" (PermitLimit, WindowSeconds); by default 10 requests per 60 seconds.
/// </summary>
public static class AuthRateLimiting
{
    /// <summary>
    /// The policy name, used with [EnableRateLimiting] on the limited endpoints.
    /// </summary>
    public const string PolicyName = "auth";

    private const int DefaultPermitLimit = 10;
    private const int DefaultWindowSeconds = 60;

    /// <summary>
    /// Registers the policy. Requests over the limit get 429 with a problem details body and a Retry-After header.
    /// Behind a reverse proxy the forwarded client address must be configured first (docs/architecture.md).
    /// </summary>
    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("RateLimiting:Auth");
        var permitLimit = section.GetValue("PermitLimit", DefaultPermitLimit);
        var window = TimeSpan.FromSeconds(section.GetValue("WindowSeconds", DefaultWindowSeconds));

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(PolicyName, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window, QueueLimit = 0 }));
            options.OnRejected = async (rejected, cancellationToken) =>
            {
                var http = rejected.HttpContext;
                var retryAfter = rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var wait) ? wait : window;
                http.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

                var problem = http.RequestServices.GetRequiredService<ProblemDetailsFactory>().CreateProblemDetails(
                    http, StatusCodes.Status429TooManyRequests, title: "Too Many Requests", detail: "Too many attempts. Please try again later.");
                await http.Response.WriteAsJsonAsync(problem, options: null, "application/problem+json", cancellationToken);
            };
        });
    }
}
