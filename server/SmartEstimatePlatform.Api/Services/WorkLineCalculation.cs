using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// The rounded labour cost of one estimate work line and the calculation of each of its material lines.
/// </summary>
public sealed record WorkLineCalculation(
    EstimateWorkLine Line, decimal LabourCost, IReadOnlyList<MaterialLineCalculation> MaterialLines);
