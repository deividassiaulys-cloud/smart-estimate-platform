using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SmartEstimatePlatform.Api.Models.DTOs.Auth;
using SmartEstimatePlatform.Api.Models.DTOs.Materials;
using SmartEstimatePlatform.Api.Models.DTOs.Team;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.ErrorResponseTests;

/// <summary>
/// Pins the error responses (status, title, type, detail, extensions) as they were before the
/// Article X refactor (feature 013), so the refactor can prove it changes none of them.
/// Invalid requests (400) only pin the status and that nothing was saved, because their body format may change.
/// </summary>
public sealed class ErrorResponseTests : IDisposable
{
    private const string ManagerEmail = "alpha@example.com";
    private const string ManagerPassword = "Secret123";

    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Login_WrongPassword_Returns401WithGenericMessage()
    {
        await _factory.RegisterCompanyAsync("Alpha UAB", ManagerEmail);

        var response = await _factory.LoginAsync(ManagerEmail, "Wrong1234");

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "Unauthorized",
            "https://tools.ietf.org/html/rfc9110#section-15.5.2", "Invalid email or password.");
    }

    [Fact]
    public async Task Login_FifthFailure_Returns423WithMinutesRemaining()
    {
        await _factory.RegisterCompanyAsync("Alpha UAB", ManagerEmail);
        for (var i = 0; i < 4; i++)
        {
            await _factory.LoginAsync(ManagerEmail, "Wrong1234");
        }

        var response = await _factory.LoginAsync(ManagerEmail, "Wrong1234");

        var problem = await AssertProblemAsync(response, (HttpStatusCode)423, null, null,
            "The account is locked. Try again in 15 minute(s).");
        Assert.Equal(15, ((JsonElement)problem.Extensions["minutesRemaining"]!).GetInt32());
    }

    [Fact]
    public async Task Register_ExistingEmail_Returns409()
    {
        await _factory.RegisterCompanyAsync("Alpha UAB", ManagerEmail);

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", RegisterRequest(ManagerEmail, ManagerPassword));

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "Conflict",
            "https://tools.ietf.org/html/rfc9110#section-15.5.10", "An account with this email already exists.");
    }

    [Fact]
    public async Task CreateViewer_ExistingEmail_Returns409()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", ManagerEmail);

        var response = await company.Client.PostAsJsonAsync("/api/team/viewers", ViewerRequest(ManagerEmail, ManagerPassword));

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "Conflict",
            "https://tools.ietf.org/html/rfc9110#section-15.5.10", "An account with this email already exists.");
    }

    [Fact]
    public async Task CreateMaterial_Duplicate_Returns409()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", ManagerEmail);
        await company.Client.PostAsJsonAsync("/api/materials", MaterialRequest("Cementas", "kg", 1));

        var response = await company.Client.PostAsJsonAsync("/api/materials", MaterialRequest("cementas", "kg", 2));

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "Conflict",
            "https://tools.ietf.org/html/rfc9110#section-15.5.10", "A material with this name and unit already exists.");
    }

    [Fact]
    public async Task MissingMaterial_Returns404()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", ManagerEmail);

        var response = await company.Client.GetAsync("/api/materials/999");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "Not found",
            "https://tools.ietf.org/html/rfc9110#section-15.5.5", "Material not found.");
    }

    [Fact]
    public async Task MissingViewer_Returns404()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", ManagerEmail);

        var response = await company.Client.DeleteAsync("/api/team/viewers/999");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "Not found",
            "https://tools.ietf.org/html/rfc9110#section-15.5.5", "Viewer not found.");
    }

    [Theory]
    [InlineData("Abc1234")]
    [InlineData(ManagerEmail)]
    public async Task Register_WeakPassword_Returns400AndCreatesNothing(string password)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", RegisterRequest(ManagerEmail, password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.LoginAsync(ManagerEmail, password)).StatusCode);
    }

    private static async Task<ProblemDetails> AssertProblemAsync(
        HttpResponseMessage response, HttpStatusCode status, string? title, string? type, string detail)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
        Assert.Equal((int)status, problem.Status);
        Assert.Equal(title, problem.Title);
        Assert.Equal(type, problem.Type);
        Assert.Equal(detail, problem.Detail);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
        return problem;
    }

    private static RegisterRequest RegisterRequest(string email, string password) => new()
    {
        CompanyName = "Beta UAB",
        FirstName = "Test",
        LastName = "Manager",
        Email = email,
        Password = password
    };

    private static CreateViewerRequest ViewerRequest(string email, string password) => new()
    {
        FirstName = "Ona",
        LastName = "Onaitė",
        Email = email,
        Password = password
    };

    private static MaterialRequest MaterialRequest(string name, string unit, decimal price) => new()
    {
        Name = name,
        Unit = unit,
        Price = price
    };
}
