using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// An estimate as listed on Project Detail for a Manager: its name, status and total with VAT.
/// </summary>
public sealed record EstimateSummaryResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public EstimateStatus Status { get; init; }

    public decimal Total { get; init; }
}
