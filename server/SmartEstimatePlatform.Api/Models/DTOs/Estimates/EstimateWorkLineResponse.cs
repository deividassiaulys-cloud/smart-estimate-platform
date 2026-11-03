namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// One work line of an estimate: its copied data, quantity, rounded labour cost and generated material lines.
/// </summary>
public sealed record EstimateWorkLineResponse
{
    public int Id { get; init; }

    public string WorkName { get; init; } = string.Empty;

    public string WorkUnit { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    public decimal LabourNorm { get; init; }

    public decimal LabourRate { get; init; }

    public decimal LabourCost { get; init; }

    public List<EstimateMaterialLineResponse> MaterialLines { get; init; } = [];
}
