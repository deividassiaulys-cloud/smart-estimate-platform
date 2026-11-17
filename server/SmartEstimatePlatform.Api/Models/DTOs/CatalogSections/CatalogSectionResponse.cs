namespace SmartEstimatePlatform.Api.Models.DTOs.CatalogSections;

/// <summary>
/// A catalog section as shown in the Manager's catalog, with its works in order.
/// </summary>
public sealed record CatalogSectionResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public List<CatalogSectionLineResponse> Lines { get; init; } = [];
}
