namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// The only rounding of money in the platform (BR-017): to 2 decimal places, with an exact half rounded away
/// from zero, so 0.005 becomes 0.01. .NET's default (banker's) rounding would give 0.00 and is never used.
/// </summary>
public static class MoneyRounding
{
    /// <summary>
    /// Rounds a money amount to 2 decimal places, halves away from zero.
    /// </summary>
    public static decimal Round(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}
