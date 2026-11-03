using System.ComponentModel.DataAnnotations;
using System.Globalization;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;

namespace SmartEstimatePlatform.Api.Tests.RequestValidationTests;

/// <summary>
/// The estimate input rules declared on the requests (Constitution X.10), checked exactly as the API checks a body.
/// </summary>
public sealed class EstimateRequestValidationTests
{
    [Theory]
    [InlineData("0.001", true)]
    [InlineData("35", true)]
    [InlineData("0", false)]
    [InlineData("-1", false)]
    [InlineData("1.2345", false)]
    public void Quantity_MustBeGreaterThan0WithAtMost3Decimals(string quantity, bool valid)
    {
        var value = decimal.Parse(quantity, CultureInfo.InvariantCulture);

        Assert.Equal(valid, IsValid(new AddWorkLineRequest { WorkId = 1, Quantity = value }));
        Assert.Equal(valid, IsValid(new ChangeQuantityRequest { Quantity = value }));
    }

    [Theory]
    [InlineData("0", true)]
    [InlineData("100", true)]
    [InlineData("12.34", true)]
    [InlineData("-0.01", false)]
    [InlineData("100.01", false)]
    [InlineData("12.345", false)]
    public void EachPercentage_MustBeFrom0To100WithAtMost2Decimals(string percent, bool valid)
    {
        var value = decimal.Parse(percent, CultureInfo.InvariantCulture);
        var valid0 = new ChangePercentagesRequest { OverheadPercent = 10, ProfitPercent = 15, VatPercent = 21 };

        Assert.Equal(valid, IsValid(valid0 with { OverheadPercent = value }));
        Assert.Equal(valid, IsValid(valid0 with { ProfitPercent = value }));
        Assert.Equal(valid, IsValid(valid0 with { VatPercent = value }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyEstimateName_IsRejected(string name) =>
        Assert.False(IsValid(new CreateEstimateRequest { Name = name }));

    [Fact]
    public void EstimateNameOver200Characters_IsRejected() =>
        Assert.False(IsValid(new CreateEstimateRequest { Name = new string('a', 201) }));

    private static bool IsValid(object request) =>
        Validator.TryValidateObject(request, new ValidationContext(request), [], validateAllProperties: true);
}
