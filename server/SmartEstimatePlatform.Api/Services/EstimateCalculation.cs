namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Every amount of an estimate (BR-015–BR-017). Each money amount is already rounded, and the total is the sum
/// of the rounded parts. The section subtotals and the subtotal of the lines without a section add up to the direct
/// costs (FR-034).
/// </summary>
public sealed record EstimateCalculation(
    IReadOnlyList<WorkLineCalculation> WorkLines,
    IReadOnlyList<SectionCalculation> Sections,
    decimal UnsectionedSubtotal,
    decimal DirectCosts,
    decimal Overhead,
    decimal Profit,
    decimal Vat,
    decimal Total);
