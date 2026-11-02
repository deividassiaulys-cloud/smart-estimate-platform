using System.ComponentModel.DataAnnotations;

namespace SmartEstimatePlatform.Api.Models.DTOs.Auth;

/// <summary>
/// Credentials a user submits to log in.
/// </summary>
public sealed record LoginRequest
{
    /// <summary>
    /// Login email, matched case-insensitively.
    /// </summary>
    [Required]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
