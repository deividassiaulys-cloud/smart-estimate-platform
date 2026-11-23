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
    [InlineData("0.0616", "vnt.", true)]
    [InlineData("99999999999999.9999", "kg", true)]
    [InlineData("-0.01", "kg", false)]
    [InlineData("0.06164", "kg", false)]
    [InlineData("100000000000000.0000", "kg", false)]
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

    [Theory]
    [InlineData(256, true)]
    [InlineData(257, false)]
    public void RegisterRequest_EmailOver256Characters_IsRejected(int length, bool valid)
    {
        // An email is at most 256 characters, the column's size, so a longer one is a 400, not a database error (F9).
        var email = new string('a', length - "@example.com".Length) + "@example.com";

        Assert.Equal(valid, IsValid(ValidRegisterRequest() with { Email = email }));
        Assert.Equal(valid, IsValid(ValidViewerRequest() with { Email = email }));
        Assert.Equal(valid, IsValid(new LoginRequest { Email = email, Password = "Secret123" }));
    }

    [Theory]
    [InlineData("FirstName", 100)]
    [InlineData("LastName", 100)]
    [InlineData("CompanyName", 200)]
    [InlineData("Password", 128)]
    public void RegisterRequest_NamesOverTheLimit_AreRejected(string field, int limit)
    {
        RegisterRequest With(int length)
        {
            var value = new string('a', length - 3) + "1A!";
            return field switch
            {
                "FirstName" => ValidRegisterRequest() with { FirstName = value },
                "LastName" => ValidRegisterRequest() with { LastName = value },
                "CompanyName" => ValidRegisterRequest() with { CompanyName = value },
                _ => ValidRegisterRequest() with { Password = value }
            };
        }

        Assert.True(IsValid(With(limit)));
        Assert.False(IsValid(With(limit + 1)));
    }

    [Theory]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void PasswordFields_HaveAtMost128Characters(int length, bool valid)
    {
        var password = new string('a', length - 3) + "1A!";

        Assert.Equal(valid, IsValid(ValidViewerRequest() with { Password = password }));
        Assert.Equal(valid, IsValid(new ResetPasswordRequest { NewPassword = password }));
        Assert.Equal(valid, IsValid(new LoginRequest { Email = "jonas@example.com", Password = password }));
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void CreateViewerRequest_NamesHaveAtMost100Characters(int length, bool valid)
    {
        var name = new string('a', length);

        Assert.Equal(valid, IsValid(ValidViewerRequest() with { FirstName = name }));
        Assert.Equal(valid, IsValid(ValidViewerRequest() with { LastName = name }));
    }

    private static bool IsValid(object request) =>
        Validator.TryValidateObject(request, new ValidationContext(request), [], validateAllProperties: true);
}
