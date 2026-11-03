using System.Globalization;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Tests.MoneyRoundingTests;

/// <summary>
/// Money is rounded to 2 decimals with halves away from zero (BR-017), never with banker's rounding.
/// </summary>
public sealed class MoneyRoundingTests
{
    [Theory]
    [InlineData("0.005", "0.01")]
    [InlineData("0.004", "0.00")]
    [InlineData("0.015", "0.02")]
    [InlineData("40.425", "40.43")]
    [InlineData("-0.005", "-0.01")]
    [InlineData("18.375", "18.38")]
    [InlineData("12.34", "12.34")]
    public void Round_HalvesGoAwayFromZero(string amount, string expected) =>
        Assert.Equal(Parse(expected), MoneyRounding.Round(Parse(amount)));

    private static decimal Parse(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
}
