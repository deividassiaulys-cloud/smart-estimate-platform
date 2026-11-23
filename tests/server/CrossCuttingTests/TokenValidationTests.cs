using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SmartEstimatePlatform.Api.Services;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CrossCuttingTests;

/// <summary>
/// Forged tokens are refused (feature 021, F3). Each token names a real Manager, so only the forged part can be
/// the reason for the 401; <see cref="CorrectlyBuiltToken_IsAccepted"/> proves the tokens are otherwise valid.
/// </summary>
public sealed class TokenValidationTests : IDisposable
{
    private const string Issuer = "SmartEstimatePlatform";

    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    public static TheoryData<string> Forgeries() =>
        ["wrong signing key", "alg=none", "expired", "wrong issuer", "wrong audience", "changed company_id"];

    [Theory]
    [MemberData(nameof(Forgeries))]
    public async Task ForgedToken_IsRejected(string forgery)
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");

        var token = forgery switch
        {
            "wrong signing key" => Build(alpha, key: "another-signing-key-of-the-same-length-01"),
            "alg=none" => Unsigned(alpha),
            // Past the default 5-minute clock skew.
            "expired" => Build(alpha, expires: DateTime.UtcNow.AddMinutes(-10)),
            "wrong issuer" => Build(alpha, issuer: "SomeoneElse"),
            "wrong audience" => Build(alpha, audience: "SomeoneElse"),
            // A real token whose company is edited to Beta's, keeping the original signature.
            "changed company_id" => WithPayloadClaim(alpha.Token, TokenService.CompanyIdClaim, beta.CompanyId.ToString()),
            _ => throw new ArgumentOutOfRangeException(nameof(forgery))
        };

        var response = await _factory.CreateClient(token).GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CorrectlyBuiltToken_IsAccepted()
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var response = await _factory.CreateClient(Build(alpha)).GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static string Build(
        LoggedInCompany company, string key = ApiFactory.JwtKey, string issuer = Issuer, string audience = Issuer, DateTime? expires = null)
    {
        var expiry = expires ?? DateTime.UtcNow.AddMinutes(60);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = expiry.AddMinutes(-60),
            NotBefore = expiry.AddMinutes(-60),
            Expires = expiry,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(Claims(company))
        });
    }

    // The same claims, with an "alg": "none" header and no signature.
    private static string Unsigned(LoggedInCompany company) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Issuer,
            Expires = DateTime.UtcNow.AddMinutes(60),
            Subject = new ClaimsIdentity(Claims(company))
        });

    private static Claim[] Claims(LoggedInCompany company) =>
    [
        new(JwtRegisteredClaimNames.Sub, company.ManagerId.ToString()),
        new(TokenService.CompanyIdClaim, company.CompanyId.ToString()),
        new(TokenService.TokenVersionClaim, "0"),
        new(ClaimTypes.Role, "Manager")
    ];

    private static string WithPayloadClaim(string token, string claim, string value)
    {
        var parts = token.Split('.');
        var payload = JsonNode.Parse(Base64UrlEncoder.Decode(parts[1]))!;
        payload[claim] = value;
        parts[1] = Base64UrlEncoder.Encode(payload.ToJsonString());
        return string.Join('.', parts);
    }
}
