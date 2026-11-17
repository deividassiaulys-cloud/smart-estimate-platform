namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Every amount of an estimate (BR-015–BR-017). Each money amount is already rounded; Labour, Materials and Machinery
/// are the sums of the work lines' parts, the direct costs are their sum, and the total is the sum of the rounded parts.
/// </summary>
public sealed record EstimateCalculation(
    IReadOnlyList<WorkLineCalculation> WorkLines,
    decimal Labour,
    decimal Materials,
    decimal Machinery,
    decimal DirectCosts,
    decimal Overhead,
    decimal Profit,
    decimal Vat,
    decimal Total);
