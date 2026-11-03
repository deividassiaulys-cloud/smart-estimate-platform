namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Limits shared by every money and quantity value, defined once (see specs/shared-technical-rules.md).
/// </summary>
public static class NumberRules
{
    /// <summary>
    /// The largest money amount that fits DECIMAL(18,2).
    /// </summary>
    public const decimal MaxMoney = 9_999_999_999_999_999.99m;

    /// <summary>
    /// The largest quantity or norm that fits DECIMAL(18,3).
    /// </summary>
    public const decimal MaxQuantity = 999_999_999_999_999.999m;

    /// <summary>
    /// Whether the value has no more than <paramref name="places"/> decimal places. Values are checked,
    /// not rounded: what the user typed is exactly what gets stored.
    /// </summary>
    public static bool HasAtMostDecimals(decimal value, int places) => decimal.Round(value, places) == value;

    /// <summary>
    /// Why a quantity or norm that must be greater than 0 (with at most 3 decimal places, within DECIMAL(18,3))
    /// is invalid, or null when it is valid. <paramref name="label"/> names the value in the message.
    /// </summary>
    public static string? PositiveQuantityError(decimal value, string label) =>
        value <= 0 ? $"{label} must be greater than 0."
        : !HasAtMostDecimals(value, 3) ? $"{label} can have at most 3 decimal places."
        : value > MaxQuantity ? $"{label} is too large."
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
