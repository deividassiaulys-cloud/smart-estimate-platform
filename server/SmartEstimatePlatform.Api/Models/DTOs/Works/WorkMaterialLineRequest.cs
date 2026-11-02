using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.Works;

/// <summary>
/// One material line of a work as the Manager submits it.
/// </summary>
public sealed record WorkMaterialLineRequest : IValidatableObject
{
    /// <summary>
    /// A material from the Manager's own catalog.
    /// </summary>
    public int MaterialId { get; init; }

    /// <summary>
    /// Material quantity per unit of work: greater than 0, with at most 3 decimal places.
    /// </summary>
    public decimal ConsumptionNorm { get; init; }

    /// <summary>
    /// Waste percentage from 0 to 100 inclusive, with at most 2 decimal places.
    /// </summary>
    public decimal WastePercent { get; init; }

    /// <summary>
    /// Applies the <see cref="WorkRules"/> for a material line.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        WorkRules.ValidateLine(ConsumptionNorm, WastePercent);
}
