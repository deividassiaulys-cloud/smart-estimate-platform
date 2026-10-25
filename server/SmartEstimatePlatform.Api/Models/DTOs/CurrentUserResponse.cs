using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Models.DTOs;

/// <summary>
/// The logged-in user as shown on the Dashboard.
/// </summary>
public class CurrentUserResponse
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public UserRole Role { get; set; }
}
