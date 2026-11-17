using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// A catalog section to add to an estimate, with the section quantity (FR-031).
/// </summary>
public sealed record AddSectionRequest : IValidatableObject
{
    /// <summary>
    /// A section from the Manager's own catalog.
    /// </summary>
    public int CatalogSectionId { get; init; }

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
