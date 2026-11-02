namespace SmartEstimatePlatform.Api.Models.DTOs.Works;

/// <summary>
/// A work as shown in the Manager's catalog, with its full composition.
/// </summary>
public sealed record WorkResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public decimal LabourRate { get; init; }

    public decimal LabourNorm { get; init; }

    public List<WorkMaterialLineResponse> Lines { get; init; } = [];
}
