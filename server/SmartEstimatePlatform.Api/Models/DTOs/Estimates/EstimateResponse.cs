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

    /// <summary>
    /// Sum of the work lines' labour costs.
    /// </summary>
    public decimal Labour { get; init; }

    /// <summary>
    /// Sum of the work lines' Material costs.
    /// </summary>
    public decimal Materials { get; init; }

    /// <summary>
    /// Sum of the work lines' Machinery costs.
    /// </summary>
    public decimal Machinery { get; init; }

    /// <summary>
    /// Labour + Materials + Machinery.
    /// </summary>
    public decimal DirectCosts { get; init; }

    public decimal Overhead { get; init; }

    public decimal Profit { get; init; }

    public decimal Vat { get; init; }

    public decimal Total { get; init; }
}
