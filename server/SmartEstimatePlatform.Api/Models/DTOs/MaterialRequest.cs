using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Models.DTOs;

/// <summary>
/// Data a Manager submits to create or edit a material.
/// </summary>
public class MaterialRequest
{
    [Required]
    [StringLength(Material.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// One of the units returned by GET /api/units.
    /// </summary>
    [Required]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Price per unit in euros: zero or more, with at most 2 decimal places.
    /// </summary>
    public decimal Price { get; set; }
}
