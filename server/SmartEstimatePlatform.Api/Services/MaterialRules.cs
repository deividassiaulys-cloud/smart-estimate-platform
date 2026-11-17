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
    /// Returns the unit, kind and price errors: the unit must be in the fixed list, the kind one of
    /// <see cref="MaterialKind"/>, and the price zero or more, with at most 4 decimal places, and fit DECIMAL(18,4).
    /// </summary>
    public static IEnumerable<ValidationResult> Validate(string unit, MaterialKind kind, decimal price)
    {
        if (!MeasurementUnit.IsValid(unit))
        {
            yield return new ValidationResult("Unit must be one of the fixed measurement units.", ["Unit"]);
        }

        if (!Enum.IsDefined(kind))
        {
            yield return new ValidationResult("Kind must be Material or Machinery.", ["Kind"]);
        }

        if (NumberRules.UnitPriceError(price, "Price") is { } priceError)
        {
            yield return new ValidationResult(priceError, ["Price"]);
        }
    }
}
