using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.Works;

/// <summary>
/// Data a Manager submits to create or edit a work. On edit, the lines replace the whole composition.
/// </summary>
public sealed record WorkRequest : IValidatableObject
{
    [Required]
    [StringLength(WorkRules.NameMaxLength)]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// One of the units returned by GET /api/units.
    /// </summary>
    [Required]
    public string Unit { get; init; } = string.Empty;

    /// <summary>
    /// Labour rate in euros per hour: zero or more, with at most 2 decimal places.
    /// </summary>
    public decimal LabourRate { get; init; }

    /// <summary>
    /// Labour hours per unit of work: zero or more, with at most 3 decimal places.
    /// </summary>
    public decimal LabourNorm { get; init; }

    /// <summary>
    /// The work's material lines; may be empty, and the same material may appear more than once.
    /// </summary>
    [Required]
    public List<WorkMaterialLineRequest> Lines { get; init; } = [];

    /// <summary>
    /// Applies the <see cref="WorkRules"/> for the unit, labour rate and labour norm; each line validates itself.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        WorkRules.Validate(Unit, LabourRate, LabourNorm);
}
