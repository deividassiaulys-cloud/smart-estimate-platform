namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Limits shared by every unit price, norm, quantity and percentage, defined once (see specs/shared-technical-rules.md).
/// Values are checked, never rounded: what the user typed is exactly what gets stored.
/// </summary>
public static class NumberRules
{
    /// <summary>
    /// The largest unit price (material price, labour rate) that fits DECIMAL(18,4).
    /// </summary>
    public const decimal MaxUnitPrice = 99_999_999_999_999.9999m;

    /// <summary>
    /// The largest norm (consumption norm, labour norm) that fits DECIMAL(18,6).
    /// </summary>
    public const decimal MaxNorm = 999_999_999_999.999999m;

    /// <summary>
    /// The largest estimate quantity that fits DECIMAL(18,3).
    /// </summary>
    public const decimal MaxQuantity = 999_999_999_999_999.999m;

    /// <summary>
    /// Whether the value has no more than <paramref name="places"/> decimal places.
    /// </summary>
    public static bool HasAtMostDecimals(decimal value, int places) => decimal.Round(value, places) == value;

    /// <summary>
    /// Why an estimate quantity (greater than 0, at most 3 decimal places, within DECIMAL(18,3)) is invalid, or null
    /// when it is valid. <paramref name="label"/> names the value in the message.
    /// </summary>
    public static string? PositiveQuantityError(decimal value, string label) =>
        value <= 0 ? $"{label} must be greater than 0."
        : !HasAtMostDecimals(value, 3) ? $"{label} can have at most 3 decimal places."
        : value > MaxQuantity ? $"{label} is too large."
        : null;

    /// <summary>
    /// Why a unit price, such as a material price or a labour rate (0 or more, at most 4 decimal places, within
    /// DECIMAL(18,4)), is invalid, or null when it is valid. <paramref name="label"/> names the value in the message.
    /// </summary>
    public static string? UnitPriceError(decimal value, string label) =>
        value < 0 ? $"{label} cannot be negative."
        : !HasAtMostDecimals(value, 4) ? $"{label} can have at most 4 decimal places."
        : value > MaxUnitPrice ? $"{label} is too large."
        : null;

    /// <summary>
    /// Why a norm, such as a consumption norm or a labour norm (0 or more, at most 6 decimal places, within
    /// DECIMAL(18,6)), is invalid, or null when it is valid. <paramref name="label"/> names the value in the message.
    /// </summary>
    public static string? NormError(decimal value, string label) =>
        value < 0 ? $"{label} cannot be negative."
        : !HasAtMostDecimals(value, 6) ? $"{label} can have at most 6 decimal places."
        : value > MaxNorm ? $"{label} is too large."
        : null;

    /// <summary>
    /// Why a percentage (0 to 100 inclusive, at most 2 decimal places, DECIMAL(5,2)) is invalid, or null when it
    /// is valid. <paramref name="label"/> names the value in the message.
    /// </summary>
    public static string? PercentageError(decimal value, string label) =>
        value is < 0 or > 100 ? $"{label} must be between 0 and 100."
        : !HasAtMostDecimals(value, 2) ? $"{label} can have at most 2 decimal places."
        : null;
}
