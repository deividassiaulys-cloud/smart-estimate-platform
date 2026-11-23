using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace SmartEstimatePlatform.Api.Controllers;

/// <summary>
/// Logs every request refused with 403 (feature 021, F8), then lets the default handler write the response.
/// Only the user id, method and path are logged, never the body.
/// </summary>
public sealed class LoggingAuthorizationResultHandler(ILogger<LoggingAuthorizationResultHandler> logger)
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            logger.LogWarning("Access denied: user {UserId} {Method} {Path}",
                context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, context.Request.Method, context.Request.Path.Value);
        }

        return _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
