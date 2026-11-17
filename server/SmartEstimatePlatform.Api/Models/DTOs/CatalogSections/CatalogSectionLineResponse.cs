namespace SmartEstimatePlatform.Api.Models.DTOs.CatalogSections;

/// <summary>
/// One work of a catalog section, with the work's name and unit for display.
/// </summary>
public sealed record CatalogSectionLineResponse
{
    public int Id { get; init; }

    public int WorkId { get; init; }

    public string WorkName { get; init; } = string.Empty;

    public string WorkUnit { get; init; } = string.Empty;

    public decimal Coefficient { get; init; }
}
