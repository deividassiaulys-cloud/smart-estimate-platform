using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Models.DTOs;

/// <summary>
/// Result of a successful login.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT that authenticates the user on subsequent requests.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    public int UserId { get; set; }

    public int CompanyId { get; set; }

    public UserRole Role { get; set; }
}
