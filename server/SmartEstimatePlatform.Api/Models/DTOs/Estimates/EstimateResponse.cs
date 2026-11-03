using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// A whole estimate as the Manager sees it in the Estimate Editor, with every amount calculated by the server.
/// </summary>
public sealed record EstimateResponse
{
    public int Id { get; init; }

    public int ProjectId { get; init; }

    public string Name { get; init; } = string.Empty;

    public EstimateStatus Status { get; init; }

    public decimal OverheadPercent { get; init; }

    public decimal ProfitPercent { get; init; }

    public decimal VatPercent { get; init; }

    public List<EstimateWorkLineResponse> WorkLines { get; init; } = [];

    public decimal DirectCosts { get; init; }

    public decimal Overhead { get; init; }

    public decimal Profit { get; init; }

    public decimal Vat { get; init; }

    public decimal Total { get; init; }
}
