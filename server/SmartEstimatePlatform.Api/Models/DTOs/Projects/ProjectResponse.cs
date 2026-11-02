namespace SmartEstimatePlatform.Api.Models.DTOs.Projects;

/// <summary>
/// A project as shown in the Projects list and on Project Detail, to Managers and Viewers alike.
/// It contains nothing commercial.
/// </summary>
public sealed record ProjectResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}
