using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// One value a catalog refresh would change, on a work line (no material name) or one of its material lines.
/// </summary>
public sealed record RefreshChangeResponse
{
    public string WorkName { get; init; } = string.Empty;

    public string? MaterialName { get; init; }

    public RefreshedField Field { get; init; }

    public decimal OldValue { get; init; }

    public decimal NewValue { get; init; }
}
