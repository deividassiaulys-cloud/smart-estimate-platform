using System.ComponentModel.DataAnnotations;
using SmartEstimatePlatform.Api.Models.DTOs.Projects;

namespace SmartEstimatePlatform.Api.Tests.RequestValidationTests;

/// <summary>
/// The project field rules declared on the request (Constitution X.10), checked exactly as the API checks a body.
/// </summary>
public sealed class ProjectRequestValidationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyName_IsRejected(string name) =>
        Assert.False(IsValid(new ProjectRequest { Name = name }));

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void Name_HasAtMost200Characters(int length, bool valid) =>
        Assert.Equal(valid, IsValid(new ProjectRequest { Name = new string('a', length) }));

    [Fact]
    public void NoDescription_IsAccepted() =>
        Assert.True(IsValid(new ProjectRequest { Name = "Buto remontas", Description = null }));

    [Theory]
    [InlineData(2000, true)]
    [InlineData(2001, false)]
    public void Description_HasAtMost2000Characters(int length, bool valid) =>
        Assert.Equal(valid, IsValid(new ProjectRequest { Name = "Buto remontas", Description = new string('a', length) }));

    private static bool IsValid(ProjectRequest request) =>
        Validator.TryValidateObject(request, new ValidationContext(request), [], validateAllProperties: true);
}
