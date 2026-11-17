using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// An estimate section's subtotal: the sum of its work lines' labour and material costs, already rounded (FR-034).
/// </summary>
public sealed record SectionCalculation(EstimateSection Section, decimal Subtotal);
