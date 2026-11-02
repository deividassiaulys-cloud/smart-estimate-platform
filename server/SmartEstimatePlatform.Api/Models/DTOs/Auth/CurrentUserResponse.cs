using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Models.DTOs.Auth;

/// <summary>
/// The logged-in user as shown on the Dashboard.
/// </summary>
public sealed record CurrentUserResponse
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string CompanyName { get; init; } = string.Empty;

    public UserRole Role { get; init; }
}
