using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// A work from the catalog to add to an estimate, with its quantity.
/// </summary>
public sealed record AddWorkLineRequest : IValidatableObject
{
    /// <summary>
    /// A work from the Manager's own catalog.
    /// </summary>
    public int WorkId { get; init; }

    /// <summary>
    /// Units of the work: greater than 0, with at most 3 decimal places.
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// Applies the <see cref="EstimateRules"/> for the quantity.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        EstimateRules.ValidateQuantity(Quantity);
}
