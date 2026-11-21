namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// One section of an estimate with its subtotal (FR-034); its work lines carry its ID.
/// </summary>
public sealed record EstimateSectionResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    /// <summary>
    /// Sum of the section's work lines' labour and material costs, already rounded.
    /// </summary>
    public decimal Subtotal { get; init; }
}
