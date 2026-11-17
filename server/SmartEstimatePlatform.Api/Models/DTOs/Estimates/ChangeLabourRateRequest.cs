using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// A new labour rate for one estimate work line.
/// </summary>
public sealed record ChangeLabourRateRequest : IValidatableObject
{
    /// <summary>
    /// Euros per hour: 0 or more, with at most 4 decimal places.
    /// </summary>
    public decimal LabourRate { get; init; }

    /// <summary>
    /// Applies the shared unit-price rule (<see cref="NumberRules.UnitPriceError"/>).
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NumberRules.UnitPriceError(LabourRate, "Labour rate") is { } error)
        {
            yield return new ValidationResult(error, [nameof(LabourRate)]);
        }
    }
}
