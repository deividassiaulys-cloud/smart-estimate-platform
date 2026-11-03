using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// The rules for a work and its material lines, defined once and used by the requests and the database model.
/// </summary>
public static class WorkRules
{
    /// <summary>
    /// Longest work name accepted, after trimming.
    /// </summary>
    public const int NameMaxLength = 200;

    /// <summary>
    /// Returns the unit, labour rate and labour norm errors: the unit must be in the fixed list, the rate (€/h)
    /// zero or more with at most 2 decimal places, and the norm (h per unit) zero or more with at most 3.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate(string unit, decimal labourRate, decimal labourNorm)
    {
        if (!MeasurementUnit.IsValid(unit))
        {
            yield return new ValidationResult("Unit must be one of the fixed measurement units.", ["Unit"]);
        }

        if (labourRate < 0)
        {
            yield return new ValidationResult("Labour rate cannot be negative.", ["LabourRate"]);
        }
        else if (!NumberRules.HasAtMostDecimals(labourRate, 2))
        {
            yield return new ValidationResult("Labour rate can have at most 2 decimal places.", ["LabourRate"]);
        }
        else if (labourRate > NumberRules.MaxMoney)
        {
            yield return new ValidationResult("Labour rate is too large.", ["LabourRate"]);
        }

        if (labourNorm < 0)
        {
            yield return new ValidationResult("Labour norm cannot be negative.", ["LabourNorm"]);
        }
        else if (!NumberRules.HasAtMostDecimals(labourNorm, 3))
        {
            yield return new ValidationResult("Labour norm can have at most 3 decimal places.", ["LabourNorm"]);
        }
        else if (labourNorm > NumberRules.MaxQuantity)
        {
            yield return new ValidationResult("Labour norm is too large.", ["LabourNorm"]);
        }
    }

    /// <summary>
    /// Returns the errors of one material line: the consumption norm must be greater than 0 with at most
    /// 3 decimal places, and the waste percentage between 0 and 100 inclusive with at most 2 decimal places.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateLine(decimal consumptionNorm, decimal wastePercent)
    {
        if (NumberRules.PositiveQuantityError(consumptionNorm, "Consumption norm") is { } normError)
        {
            yield return new ValidationResult(normError, ["ConsumptionNorm"]);
        }

        if (NumberRules.PercentageError(wastePercent, "Waste percentage") is { } wasteError)
        {
            yield return new ValidationResult(wasteError, ["WastePercent"]);
        }
    }
}
