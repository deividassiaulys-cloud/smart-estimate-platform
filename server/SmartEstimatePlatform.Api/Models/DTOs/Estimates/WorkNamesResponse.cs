namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// One work of an estimate with the names of its materials, in order; a repeated material appears each time.
/// It deliberately has no unit, quantity, norm or price (BR-005).
/// </summary>
public sealed record WorkNamesResponse
{
    public string WorkName { get; init; } = string.Empty;

    public List<string> MaterialNames { get; init; } = [];
}
