namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// An estimate as a Viewer sees it: its name and the names of its works and their materials, and nothing
/// commercial. It has no number or status field, so prices, quantities and status cannot reach a Viewer (BR-005).
/// </summary>
public sealed record EstimateNamesResponse
{
    public string Name { get; init; } = string.Empty;

    public List<WorkNamesResponse> Works { get; init; } = [];
}
