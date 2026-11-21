namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// One estimate section as a Viewer sees it: its name and its works' names only (FR-034, BR-005).
/// </summary>
public sealed record SectionNamesResponse
{
    public string Name { get; init; } = string.Empty;

    public List<WorkNamesResponse> Works { get; init; } = [];
}
