namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// One work line of an estimate: its copied data, quantity, rounded labour cost, its Labour / Materials / Machinery
/// parts, amount and unit price (BR-015, BR-017), and its generated material lines.
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

    /// <summary>
    /// Labour hours: quantity × labour norm, not rounded.
    /// </summary>
    public decimal Hours { get; init; }

    /// <summary>
    /// Sum of the rounded costs of the Material lines.
    /// </summary>
    public decimal Materials { get; init; }

    /// <summary>
    /// Sum of the rounded costs of the Machinery lines.
    /// </summary>
    public decimal Machinery { get; init; }

    /// <summary>
    /// Labour cost + Materials + Machinery.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Amount ÷ quantity, rounded to 2 decimal places; shown only.
    /// </summary>
    public decimal UnitPrice { get; init; }

    public List<EstimateMaterialLineResponse> MaterialLines { get; init; } = [];
}
