namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Settings for issuing JWTs, bound from the "Jwt" configuration section.
/// The signing key is supplied outside the repository (user secrets or environment variables).
/// </summary>
public sealed record JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Symmetric signing key; must be at least 32 characters (256 bits) for HMAC-SHA256.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// How long an issued token stays valid.
    /// </summary>
    public int ExpiresMinutes { get; init; } = 60;
}
