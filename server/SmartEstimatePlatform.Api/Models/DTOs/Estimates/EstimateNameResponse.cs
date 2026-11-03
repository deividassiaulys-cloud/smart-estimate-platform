namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// An estimate as listed for any user of the company, Viewers included: only its ID (to open it) and name.
/// It deliberately has no status or total (BR-005).
/// </summary>
public sealed record EstimateNameResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}
