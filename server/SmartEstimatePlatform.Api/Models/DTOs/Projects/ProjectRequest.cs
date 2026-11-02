using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.Projects;

/// <summary>
/// Data a Manager submits to create or edit a project.
/// </summary>
public sealed record ProjectRequest
{
    /// <summary>
    /// Required; stored trimmed. Names need not be unique.
    /// </summary>
    [Required]
    [StringLength(ProjectRules.NameMaxLength)]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Optional; stored trimmed, and a whitespace-only description is stored as none.
    /// </summary>
    [StringLength(ProjectRules.DescriptionMaxLength)]
    public string? Description { get; init; }
}
