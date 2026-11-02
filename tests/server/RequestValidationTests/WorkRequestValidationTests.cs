using System.ComponentModel.DataAnnotations;
using System.Globalization;
using SmartEstimatePlatform.Api.Models.DTOs.Works;

namespace SmartEstimatePlatform.Api.Tests.RequestValidationTests;

/// <summary>
/// The work rules declared on the work requests (Constitution X.10), checked exactly as the API checks a
/// request body, including the lines of the composition.
/// </summary>
public sealed class WorkRequestValidationTests
{
    private static WorkRequest ValidRequest() => new()
    {
        Name = "Sienų dažymas",
        Unit = "m²",
        LabourRate = 18.50m,
        LabourNorm = 0.250m,
        Lines = [Line("0.150", "5")]
    };

    private static WorkMaterialLineRequest Line(string consumptionNorm, string wastePercent) => new()
    {
        MaterialId = 1,
        ConsumptionNorm = decimal.Parse(consumptionNorm, CultureInfo.InvariantCulture),
        WastePercent = decimal.Parse(wastePercent, CultureInfo.InvariantCulture)
    };

    [Theory]
    [InlineData("0", true)]
    [InlineData("100", true)]
    [InlineData("12.34", true)]
    [InlineData("-1", false)]
    [InlineData("101", false)]
    [InlineData("12.345", false)]
    public void WastePercent_MustBeFrom0To100WithAtMost2Decimals(string waste, bool valid) =>
        Assert.Equal(valid, IsValid(ValidRequest() with { Lines = [Line("1", waste)] }));

    [Theory]
    [InlineData("0.001", true)]
    [InlineData("0", false)]
    [InlineData("-0.001", false)]
    [InlineData("1.2345", false)]
    [InlineData("1000000000000000.000", false)]
    public void ConsumptionNorm_MustBeGreaterThan0WithAtMost3Decimals(string norm, bool valid) =>
        Assert.Equal(valid, IsValid(ValidRequest() with { Lines = [Line(norm, "0")] }));

    [Theory]
    [InlineData("0", true)]
    [InlineData("0.125", true)]
    [InlineData("-0.001", false)]
    [InlineData("0.1234", false)]
    public void LabourNorm_MayBe0WithAtMost3Decimals(string norm, bool valid) =>
        Assert.Equal(valid, IsValid(ValidRequest() with { LabourNorm = decimal.Parse(norm, CultureInfo.InvariantCulture) }));

    [Theory]
    [InlineData("0", true)]
    [InlineData("18.50", true)]
    [InlineData("-0.01", false)]
    [InlineData("1.234", false)]
    [InlineData("10000000000000000.00", false)]
    public void LabourRate_MustBe0OrMoreWithAtMost2Decimals(string rate, bool valid) =>
        Assert.Equal(valid, IsValid(ValidRequest() with { LabourRate = decimal.Parse(rate, CultureInfo.InvariantCulture) }));

    [Theory]
    [InlineData("pcs")]
    [InlineData("")]
    public void Unit_OutsideTheFixedList_IsRejected(string unit) =>
        Assert.False(IsValid(ValidRequest() with { Unit = unit }));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyName_IsRejected(string name) =>
        Assert.False(IsValid(ValidRequest() with { Name = name }));

    [Fact]
    public void NoLines_IsAccepted() =>
        Assert.True(IsValid(ValidRequest() with { Lines = [] }));

    [Fact]
    public void SameMaterialOnTwoLines_IsAccepted() =>
        Assert.True(IsValid(ValidRequest() with { Lines = [Line("0.150", "5"), Line("0.050", "0")] }));

    // The same recursive validation the API applies to a request body: the request and every line.
    private static bool IsValid(WorkRequest request) =>
        Validator.TryValidateObject(request, new ValidationContext(request), [], validateAllProperties: true)
        && request.Lines.All(line => Validator.TryValidateObject(line, new ValidationContext(line), [], validateAllProperties: true));
}
