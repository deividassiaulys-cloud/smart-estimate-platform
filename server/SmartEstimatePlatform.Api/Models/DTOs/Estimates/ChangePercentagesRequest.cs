using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// New overhead, profit and VAT percentages for an estimate, each 0–100 with at most 2 decimal places.
/// </summary>
public sealed record ChangePercentagesRequest : IValidatableObject
{
    public decimal OverheadPercent { get; init; }

    public decimal ProfitPercent { get; init; }

    public decimal VatPercent { get; init; }

    /// <summary>
    /// Applies the <see cref="EstimateRules"/> for the three percentages.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        EstimateRules.ValidatePercentages(OverheadPercent, ProfitPercent, VatPercent);
}
