namespace SmartEstimatePlatform.Api.Models.DTOs.Works;

/// <summary>
/// One material line of a work, with the material's current name and unit from the catalog.
/// </summary>
public sealed record WorkMaterialLineResponse
{
    public int Id { get; init; }

    public int MaterialId { get; init; }

    public string MaterialName { get; init; } = string.Empty;

    public string MaterialUnit { get; init; } = string.Empty;

    public decimal ConsumptionNorm { get; init; }

    public decimal WastePercent { get; init; }
}
