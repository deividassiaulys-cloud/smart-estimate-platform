using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// The calculated quantity (not rounded) and rounded cost of one estimate material line.
/// </summary>
public sealed record MaterialLineCalculation(EstimateMaterialLine Line, decimal Quantity, decimal Cost);
