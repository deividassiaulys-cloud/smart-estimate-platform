using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.Materials;

/// <summary>
/// Data a Manager submits to create or edit a material.
/// </summary>
public sealed record MaterialRequest : IValidatableObject
{
    [Required]
    [StringLength(MaterialRules.NameMaxLength)]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// One of the units returned by GET /api/units.
    /// </summary>
    [Required]
    public string Unit { get; init; } = string.Empty;

    /// <summary>
    /// Price per unit in euros: zero or more, with at most 4 decimal places.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// Material or Machinery (FR-030); Material when omitted.
    /// </summary>
    public MaterialKind Kind { get; init; } = MaterialKind.Material;

    /// <summary>
    /// Applies the <see cref="MaterialRules"/> for the unit, the kind and the price; the name is checked by the attributes above.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        MaterialRules.Validate(Unit, Kind, Price);
}
