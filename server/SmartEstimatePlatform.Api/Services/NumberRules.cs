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
}
