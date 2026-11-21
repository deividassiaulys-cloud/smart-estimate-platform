using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// A new quantity for an estimate section (FR-032).
/// </summary>
public sealed record ChangeSectionQuantityRequest : IValidatableObject
{
    /// <summary>
    /// Units of the section: greater than 0, with at most 3 decimal places.
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// Applies the <see cref="EstimateRules"/> for a quantity.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        EstimateRules.ValidateQuantity(Quantity);
}
