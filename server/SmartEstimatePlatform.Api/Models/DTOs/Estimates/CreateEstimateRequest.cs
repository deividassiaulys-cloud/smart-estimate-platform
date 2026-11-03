using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// Data a Manager submits to create an estimate in a project.
/// </summary>
public sealed record CreateEstimateRequest
{
    /// <summary>
    /// Required; stored trimmed.
    /// </summary>
    [Required]
    [StringLength(EstimateRules.NameMaxLength)]
    public string Name { get; init; } = string.Empty;
}
