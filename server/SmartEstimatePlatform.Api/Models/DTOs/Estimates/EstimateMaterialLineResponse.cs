namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// One generated material line: its copied data, its calculated quantity (not rounded) and its rounded cost.
/// </summary>
public sealed record EstimateMaterialLineResponse
{
    public int Id { get; init; }

    public string MaterialName { get; init; } = string.Empty;

    public string MaterialUnit { get; init; } = string.Empty;

    public decimal ConsumptionNorm { get; init; }

    public decimal WastePercent { get; init; }

    public decimal Quantity { get; init; }

    public decimal Price { get; init; }

    public decimal Cost { get; init; }
}
