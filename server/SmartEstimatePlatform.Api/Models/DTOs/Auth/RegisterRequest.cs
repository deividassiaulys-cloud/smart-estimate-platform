using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.Auth;

/// <summary>
/// Data a visitor submits to register; creates their company and their Manager account together.
/// </summary>
public sealed record RegisterRequest : IValidatableObject
{
    /// <summary>
    /// Name of the company to create. Not required to be unique across companies.
    /// </summary>
    [Required]
    public string CompanyName { get; init; } = string.Empty;

    [Required]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Login email for the new Manager account. Must not already be in use (case-insensitive).
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Plain-text password. Only its hash is stored; weak passwords are rejected.
    /// </summary>
    [Required]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Applies the <see cref="PasswordRules"/>, comparing the password with this request's email.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PasswordRules.Validate(Password, Email) is { } error)
        {
            yield return new ValidationResult(error, [nameof(Password)]);
        }
    }
}
