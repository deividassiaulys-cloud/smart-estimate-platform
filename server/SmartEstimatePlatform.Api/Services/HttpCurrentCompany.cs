using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Reads the current company from the authenticated user's token claims.
/// Token validation has already confirmed that the claim matches the user record.
/// </summary>
public class HttpCurrentCompany(IHttpContextAccessor httpContextAccessor) : ICurrentCompany
{
    public int? CompanyId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return int.TryParse(user.FindFirst(TokenService.CompanyIdClaim)?.Value, out var companyId)
                ? companyId
                : null;
        }
    }
}
