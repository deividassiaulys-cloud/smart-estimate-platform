namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// A line whose work (no material name) or material is not in the catalog; a refresh leaves it unchanged.
/// </summary>
public sealed record RefreshMissResponse
{
    public string WorkName { get; init; } = string.Empty;

    public string? MaterialName { get; init; }
}
