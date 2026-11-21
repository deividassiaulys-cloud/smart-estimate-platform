using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.CatalogSections;

/// <summary>
/// One work of a catalog section as the Manager submits it.
/// </summary>
public sealed record CatalogSectionLineRequest : IValidatableObject
{
    /// <summary>
    /// A work from the Manager's own catalog.
    /// </summary>
    public int WorkId { get; init; }

    /// <summary>
    /// Work quantity per 1 unit of the section: greater than 0, with at most 6 decimal places.
    /// </summary>
    public decimal Coefficient { get; init; }

    /// <summary>
    /// Applies the <see cref="CatalogSectionRules"/> for a line.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        CatalogSectionRules.ValidateLine(Coefficient);
}
