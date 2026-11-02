using System.IdentityModel.Tokens.Jwt;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Reads the current user and company from the authenticated user's token claims.
/// Token validation has already confirmed that the claims match the user record.
/// </summary>
public class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public int? UserId => ReadClaim(JwtRegisteredClaimNames.Sub);

    public int? CompanyId => ReadClaim(TokenService.CompanyIdClaim);

    private int? ReadClaim(string type)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return int.TryParse(user.FindFirst(type)?.Value, out var value) ? value : null;
    }
}
