using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Issues JWTs that identify a user, their company, and their role.
/// </summary>
public class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    /// <summary>
    /// Claim holding the user's company ID.
    /// </summary>
    public const string CompanyIdClaim = "company_id";

    /// <summary>
    /// Claim holding the user's <see cref="User.TokenVersion"/> when the token was issued.
    /// </summary>
    public const string TokenVersionClaim = "token_version";

    /// <inheritdoc />
    public string CreateToken(User user)
    {
        var jwt = options.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = DateTime.UtcNow.AddMinutes(jwt.ExpiresMinutes),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(CompanyIdClaim, user.CompanyId.ToString()),
                new Claim(TokenVersionClaim, user.TokenVersion.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            ])
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
