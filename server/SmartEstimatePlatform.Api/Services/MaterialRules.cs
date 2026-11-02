using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// The rules for a material in a company's catalog, defined once and used by the request, the service and the database model.
/// </summary>
public static class MaterialRules
{
    /// <summary>
    /// Longest material name accepted, after trimming.
    /// </summary>
    public const int NameMaxLength = 200;

    /// <summary>
    /// The largest price that fits DECIMAL(18,2).
    /// </summary>
    public const decimal MaxPrice = 9_999_999_999_999_999.99m;

    /// <summary>
    /// Returns the unit and price errors: the unit must be in the fixed list, and the price must be zero or more,
    /// with at most 2 decimal places, and fit DECIMAL(18,2).
    /// </summary>
    public static IEnumerable<ValidationResult> Validate(string unit, decimal price)
    {
        if (!MeasurementUnit.IsValid(unit))
        {
            yield return new ValidationResult("Unit must be one of the fixed measurement units.", ["Unit"]);
        }

        if (price < 0)
        {
            yield return new ValidationResult("Price cannot be negative.", ["Price"]);
        }
        // Checked, not rounded: what the Manager typed is exactly what gets stored.
        else if (decimal.Round(price, 2) != price)
        {
            yield return new ValidationResult("Price can have at most 2 decimal places.", ["Price"]);
        }
        else if (price > MaxPrice)
        {
            yield return new ValidationResult("Price is too large.", ["Price"]);
        }
    }
}
