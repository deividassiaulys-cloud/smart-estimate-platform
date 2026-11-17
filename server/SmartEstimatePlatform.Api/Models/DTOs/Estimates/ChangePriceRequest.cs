using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// A new price for one estimate material line.
/// </summary>
public sealed record ChangePriceRequest : IValidatableObject
{
    /// <summary>
    /// Euros per unit of the material: 0 or more, with at most 4 decimal places.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// Applies the shared unit-price rule (<see cref="NumberRules.UnitPriceError"/>).
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NumberRules.UnitPriceError(Price, "Price") is { } error)
        {
            yield return new ValidationResult(error, [nameof(Price)]);
        }
    }
}
