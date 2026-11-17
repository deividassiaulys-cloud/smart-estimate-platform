using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// The rules for a catalog section and its lines (FR-030), defined once and used by the requests and the database model.
/// </summary>
public static class CatalogSectionRules
{
    /// <summary>
    /// Longest catalog section name accepted, after trimming.
    /// </summary>
    public const int NameMaxLength = 200;

    /// <summary>
    /// Returns the unit and lines errors: the unit must be in the fixed list, and the section needs at least one work.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate(string unit, int lineCount)
    {
        if (!MeasurementUnit.IsValid(unit))
        {
            yield return new ValidationResult("Unit must be one of the fixed measurement units.", ["Unit"]);
        }

        if (lineCount == 0)
        {
            yield return new ValidationResult("A section needs at least one work.", ["Lines"]);
        }
    }

    /// <summary>
    /// Returns the error of one line's coefficient: greater than 0, then the shared norm rule (at most 6 decimal places,
    /// within DECIMAL(18,6)), since a coefficient is a norm of work per section unit.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateLine(decimal coefficient)
    {
        if (coefficient <= 0)
        {
            yield return new ValidationResult("Coefficient must be greater than 0.", ["Coefficient"]);
        }
        else if (NumberRules.NormError(coefficient, "Coefficient") is { } error)
        {
            yield return new ValidationResult(error, ["Coefficient"]);
        }
    }
}
