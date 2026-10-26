using System.ComponentModel.DataAnnotations;

namespace SmartEstimatePlatform.Api.Models.DTOs;

/// <summary>
/// Credentials a user submits to log in.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Login email, matched case-insensitively.
    /// </summary>
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
