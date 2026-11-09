namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// What updating an estimate from the catalog would change, shown before the Manager confirms (FR-024).
/// </summary>
public sealed record EstimateRefreshPreviewResponse
{
    public List<RefreshChangeResponse> Changes { get; init; } = [];

    public List<RefreshMissResponse> NotFound { get; init; } = [];

    /// <summary>
    /// The estimate's total with VAT now.
    /// </summary>
    public decimal CurrentTotal { get; init; }

    /// <summary>
    /// The total with VAT after the update.
    /// </summary>
    public decimal NewTotal { get; init; }
}
