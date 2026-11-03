using System.ComponentModel.DataAnnotations;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// The rules for an estimate's inputs, defined once and used by the requests, the service and the database model.
/// </summary>
public static class EstimateRules
{
    /// <summary>
    /// Longest estimate name accepted, after trimming.
    /// </summary>
    public const int NameMaxLength = 200;

    /// <summary>
    /// VAT a new estimate starts with (Lithuania's standard rate); overhead and profit start at 0.
    /// </summary>
    public const decimal DefaultVatPercent = 21m;

    /// <summary>
    /// Returns the error of a work quantity: greater than 0, with at most 3 decimal places.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateQuantity(decimal quantity)
    {
        if (NumberRules.PositiveQuantityError(quantity, "Quantity") is { } error)
        {
            yield return new ValidationResult(error, ["Quantity"]);
        }
    }

    /// <summary>
    /// Returns the errors of the overhead, profit and VAT percentages: each 0–100, with at most 2 decimal places.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidatePercentages(decimal overheadPercent, decimal profitPercent, decimal vatPercent)
    {
        (decimal Value, string Label, string Member)[] percentages =
        [
            (overheadPercent, "Overhead percentage", "OverheadPercent"),
            (profitPercent, "Profit percentage", "ProfitPercent"),
            (vatPercent, "VAT percentage", "VatPercent")
        ];

        foreach (var (value, label, member) in percentages)
        {
            if (NumberRules.PercentageError(value, label) is { } error)
            {
                yield return new ValidationResult(error, [member]);
            }
        }
    }
}
