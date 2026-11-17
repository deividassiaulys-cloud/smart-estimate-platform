using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// One estimate work line's amounts (BR-015): its rounded labour cost, the calculation of each material line, the
/// sums of its Material and Machinery line costs, its amount (Labour + Materials + Machinery) and its unit price
/// (amount ÷ quantity, rounded to 2 decimal places, shown only, BR-017). Hours = quantity × labour norm, not rounded.
/// </summary>
public sealed record WorkLineCalculation(
    EstimateWorkLine Line,
    decimal Hours,
    decimal LabourCost,
    IReadOnlyList<MaterialLineCalculation> MaterialLines,
    decimal Materials,
    decimal Machinery,
    decimal Amount,
    decimal UnitPrice);
