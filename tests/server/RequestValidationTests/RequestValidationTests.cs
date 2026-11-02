using System.ComponentModel.DataAnnotations;
using System.Globalization;
using SmartEstimatePlatform.Api.Models.DTOs.Auth;
using SmartEstimatePlatform.Api.Models.DTOs.Materials;
using SmartEstimatePlatform.Api.Models.DTOs.Team;

namespace SmartEstimatePlatform.Api.Tests.RequestValidationTests;

/// <summary>
/// The validation declared on the request records (Constitution X.10), checked exactly as the API checks
/// a request body before any service runs.
/// </summary>
public sealed class RequestValidationTests
{
    private static RegisterRequest ValidRegisterRequest() => new()
    {
        CompanyName = "Statybos UAB",
        FirstName = "Jonas",
        LastName = "Jonaitis",
        Email = "jonas@example.com",
        Password = "Secret123"
    };

    private static CreateViewerRequest ValidViewerRequest() => new()
    {
        FirstName = "Ona",
        LastName = "Onaitė",
        Email = "ona@example.com",
        Password = "Secret123"
    };

    [Fact]
    public void RegisterRequest_PasswordShorterThan8Characters_IsRejected() =>
        Assert.False(IsValid(ValidRegisterRequest() with { Password = "Abc1234" }));

    [Fact]
    public void RegisterRequest_PasswordExactly8Characters_IsAccepted() =>
        Assert.True(IsValid(ValidRegisterRequest() with { Password = "Abc12345" }));

    [Theory]
    [InlineData("jonas@example.com")]
    [InlineData("Jonas@Example.com")]
    public void RegisterRequest_PasswordEqualToEmail_IsRejected(string password) =>
        Assert.False(IsValid(ValidRegisterRequest() with { Password = password }));

    [Theory]
    [InlineData("Short1")]
    [InlineData("ona@example.com")]
    public void CreateViewerRequest_WeakPassword_IsRejected(string password) =>
        Assert.False(IsValid(ValidViewerRequest() with { Password = password }));

    [Fact]
    public void CreateViewerRequest_StrongPassword_IsAccepted() =>
        Assert.True(IsValid(ValidViewerRequest()));

    [Theory]
    [InlineData("Short1", false)]
    [InlineData("NewSecret456", true)]
    public void ResetPasswordRequest_ChecksTheLength(string password, bool valid) =>
        Assert.Equal(valid, IsValid(new ResetPasswordRequest { NewPassword = password }));

    [Theory]
    [InlineData("0", "kg", true)]
    [InlineData("12.34", "kg", true)]
    [InlineData("-0.01", "kg", false)]
    [InlineData("12.345", "kg", false)]
    [InlineData("10000000000000000.00", "kg", false)]
    [InlineData("1", "pcs", false)]
    [InlineData("1", "", false)]
    public void MaterialRequest_ChecksUnitAndPrice(string price, string unit, bool valid) =>
        Assert.Equal(valid, IsValid(new MaterialRequest
        {
            Name = "Cementas",
            Unit = unit,
            Price = decimal.Parse(price, CultureInfo.InvariantCulture)
        }));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MaterialRequest_EmptyName_IsRejected(string name) =>
        Assert.False(IsValid(new MaterialRequest { Name = name, Unit = "kg", Price = 1 }));

    private static bool IsValid(object request) =>
        Validator.TryValidateObject(request, new ValidationContext(request), [], validateAllProperties: true);
}
