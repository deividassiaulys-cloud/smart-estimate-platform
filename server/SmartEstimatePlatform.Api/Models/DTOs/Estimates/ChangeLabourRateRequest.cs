using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// A new labour rate for one estimate work line.
/// </summary>
public sealed record ChangeLabourRateRequest : IValidatableObject
{
    /// <summary>
    /// Euros per hour: 0 or more, with at most 2 decimal places.
    /// </summary>
    public decimal LabourRate { get; init; }

    /// <summary>
    /// Applies the shared money rule (<see cref="NumberRules.MoneyError"/>).
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NumberRules.MoneyError(LabourRate, "Labour rate") is { } error)
        {
            yield return new ValidationResult(error, [nameof(LabourRate)]);
        }
    }
}
