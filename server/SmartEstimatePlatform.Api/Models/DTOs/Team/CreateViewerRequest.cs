using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.Team;

/// <summary>
/// Data a Manager submits to create a Viewer account in their own company.
/// </summary>
public sealed record CreateViewerRequest : IValidatableObject
{
    [Required]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Login email for the Viewer. Must not be in use by any account in any company (case-insensitive).
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Password chosen by the Manager and passed on to the Viewer in person. Only its hash is stored.
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
