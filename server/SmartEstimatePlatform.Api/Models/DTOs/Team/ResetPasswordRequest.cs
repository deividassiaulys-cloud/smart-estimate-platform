using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.Team;

/// <summary>
/// New password a Manager sets for one of their Viewers.
/// </summary>
public sealed record ResetPasswordRequest : IValidatableObject
{
    [Required]
    public string NewPassword { get; init; } = string.Empty;

    /// <summary>
    /// Applies the <see cref="PasswordRules"/> length rule. The request carries no email; the Viewer's stored
    /// email is checked by the service.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PasswordRules.Validate(NewPassword, email: null) is { } error)
        {
            yield return new ValidationResult(error, [nameof(NewPassword)]);
        }
    }
}
