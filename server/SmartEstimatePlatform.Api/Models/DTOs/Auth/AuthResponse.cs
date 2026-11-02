using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Models.DTOs.Auth;

/// <summary>
/// Result of a successful registration or login: the user is logged in.
/// </summary>
public sealed record AuthResponse
{
    /// <summary>
    /// JWT that authenticates the user on subsequent requests.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    public int UserId { get; init; }

    public int CompanyId { get; init; }

    public UserRole Role { get; init; }
}
