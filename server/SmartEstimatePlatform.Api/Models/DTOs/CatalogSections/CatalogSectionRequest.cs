using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.CatalogSections;

/// <summary>
/// Data a Manager submits to create or edit a catalog section (FR-030). On edit, the lines replace the whole list.
/// </summary>
public sealed record CatalogSectionRequest : IValidatableObject
{
    [Required]
    [StringLength(CatalogSectionRules.NameMaxLength)]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// One of the units returned by GET /api/units; the section quantity is entered in it.
    /// </summary>
    [Required]
    public string Unit { get; init; } = string.Empty;

    /// <summary>
    /// The section's works in order; at least one, and the same work may appear more than once.
    /// </summary>
    [Required]
    public List<CatalogSectionLineRequest> Lines { get; init; } = [];

    /// <summary>
    /// Applies the <see cref="CatalogSectionRules"/> for the unit and the lines; each line validates itself.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        CatalogSectionRules.Validate(Unit, Lines.Count);
}
