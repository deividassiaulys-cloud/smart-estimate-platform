using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

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
    [StringLength(PersonRules.EmailMaxLength)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(PasswordRules.MaxLength)]
    public string Password { get; init; } = string.Empty;
}
