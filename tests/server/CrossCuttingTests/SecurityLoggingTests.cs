using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using SmartEstimatePlatform.Api.Models.DTOs.Team;
using SmartEstimatePlatform.Api.Tests.TestSupport;
using static SmartEstimatePlatform.Api.Tests.TestSupport.CatalogRequests;

namespace SmartEstimatePlatform.Api.Tests.CrossCuttingTests;

/// <summary>
/// Security events are logged with ids only, never a password, token or request body (feature 021, F8).
/// </summary>
public sealed class SecurityLoggingTests : IDisposable
{
    private const string Password = "Secret123";

    private readonly ApiFactory _factory = new();
    private readonly CapturingLoggerProvider _logs = new();
    private readonly WebApplicationFactory<Program> _logged;

    public SecurityLoggingTests() =>
        _logged = _factory.WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.AddProvider(_logs)));

    public void Dispose()
    {
        _logged.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task FailedLogin_IsLoggedWithoutThePassword()
    {
        var company = await RegisterAsync("Alpha UAB", "alpha@example.com");

        await LoginAsync("alpha@example.com", "Wrong-Password-1");
        await LoginAsync("nobody@example.com", "Other-Password-2");

        var failed = Warnings("Failed login");
        Assert.Contains(failed, e => e.Values.GetValueOrDefault("UserId") == company.ManagerId.ToString());
        Assert.Contains(failed, e => e.Values.TryGetValue("EmailHash", out var hash) && hash!.Length == 12);
        AssertNoSecret("Wrong-Password-1", "Other-Password-2", "nobody@example.com", company.Token);
    }

    [Fact]
    public async Task RemoveViewer_IsLogged()
    {
        var company = await RegisterAsync("Alpha UAB", "alpha@example.com");
        var viewerId = await CreateViewerAsync(company);

        (await company.Client.DeleteAsync($"/api/team/viewers/{viewerId}")).EnsureSuccessStatusCode();

        var entry = Assert.Single(Entries(LogLevel.Information, "Viewer removed"));
        Assert.Equal((viewerId.ToString(), company.ManagerId.ToString()), (entry.Values["ViewerId"], entry.Values["UserId"]));
    }

    [Fact]
    public async Task ViewerCallingAManagerEndpoint_IsLogged()
    {
        var company = await RegisterAsync("Alpha UAB", "alpha@example.com");
        var viewerId = await CreateViewerAsync(company);
        var viewer = await LoginClientAsync("ona@example.com");

        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync("/api/materials")).StatusCode);

        var entry = Assert.Single(Warnings("Access denied"));
        Assert.Equal((viewerId.ToString(), "GET", "/api/materials"), (entry.Values["UserId"], entry.Values["Method"], entry.Values["Path"]));
    }

    [Fact]
    public async Task AnotherCompanysId_IsLoggedAsNotFound()
    {
        var alpha = await RegisterAsync("Alpha UAB", "alpha@example.com");
        var material = await alpha.CreateMaterialAsync("Cementas");
        var beta = await RegisterAsync("Beta UAB", "beta@example.com");

        var response = await beta.Client.PutAsJsonAsync($"/api/materials/{material.Id}",
            new SmartEstimatePlatform.Api.Models.DTOs.Materials.MaterialRequest { Name = "Slapta-reiksme", Unit = "kg", Price = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var entry = Assert.Single(Warnings("Not found"));
        Assert.Equal((beta.ManagerId.ToString(), "PUT", $"/api/materials/{material.Id}"),
            (entry.Values["UserId"], entry.Values["Method"], entry.Values["Path"]));
        AssertNoSecret("Slapta-reiksme");
    }

    [Fact]
    public async Task EveryOtherSecurityEvent_IsLogged()
    {
        var company = await RegisterAsync("Alpha UAB", "alpha@example.com");
        var viewerId = await CreateViewerAsync(company);
        for (var i = 0; i < 5; i++)
        {
            await LoginAsync("ona@example.com", "Wrong1234");
        }

        (await company.Client.PatchAsJsonAsync($"/api/team/viewers/{viewerId}/password",
            new ResetPasswordRequest { NewPassword = "NewSecret456" })).EnsureSuccessStatusCode();
        var estimate = await company.CreateEstimateAsync();
        (await company.Client.ConfirmEstimateAsync(estimate.Id)).EnsureSuccessStatusCode();
        var material = await company.CreateMaterialAsync("Cementas");
        var work = await company.CreateWorkAsync(WorkRequest("Dažymas"));
        var section = await company.CreateCatalogSectionAsync(CatalogSectionRequest("Siena", (work.Id, 1m)));
        foreach (var path in new[]
                 {
                     $"/api/catalog-sections/{section.Id}", $"/api/works/{work.Id}",
                     $"/api/materials/{material.Id}", $"/api/projects/{estimate.ProjectId}"
                 })
        {
            (await company.Client.DeleteAsync(path)).EnsureSuccessStatusCode();
        }

        Assert.Single(Warnings("Account locked"));
        Assert.Single(Entries(LogLevel.Information, "Viewer password reset"));
        Assert.Single(Entries(LogLevel.Information, "Estimate confirmed"));
        foreach (var deleted in new[] { "Catalog section deleted", "Work deleted", "Material deleted", "Project deleted" })
        {
            var entry = Assert.Single(Entries(LogLevel.Information, deleted));
            Assert.Equal(company.ManagerId.ToString(), entry.Values["UserId"]);
        }

        AssertNoSecret("NewSecret456", "Wrong1234", company.Token);
    }

    private IEnumerable<LogEntry> Entries(LogLevel level, string startsWith) =>
        _logs.Entries.Where(e => e.Level == level && e.Category.StartsWith("SmartEstimatePlatform") && e.Message.StartsWith(startsWith));

    private IEnumerable<LogEntry> Warnings(string startsWith) => Entries(LogLevel.Warning, startsWith);

    private void AssertNoSecret(params string[] secrets) =>
        Assert.All(_logs.Entries, entry => Assert.All(secrets, secret => Assert.DoesNotContain(secret, entry.Message)));

    private async Task<LoggedInCompany> RegisterAsync(string companyName, string email)
    {
        var client = _logged.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new SmartEstimatePlatform.Api.Models.DTOs.Auth.RegisterRequest
        {
            CompanyName = companyName, FirstName = "Test", LastName = "Manager", Email = email, Password = Password
        });
        response.EnsureSuccessStatusCode();
        var registered = (await response.Content.ReadFromJsonAsync<SmartEstimatePlatform.Api.Models.DTOs.Auth.AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", registered.Token);
        return new LoggedInCompany(client, registered.UserId, registered.CompanyId, registered.Token);
    }

    private async Task<int> CreateViewerAsync(LoggedInCompany company)
    {
        var response = await company.Client.PostAsJsonAsync("/api/team/viewers", new CreateViewerRequest
        {
            FirstName = "Ona", LastName = "Onaitė", Email = "ona@example.com", Password = Password
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TeamMemberResponse>())!.Id;
    }

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        _logged.CreateClient().PostAsJsonAsync("/api/auth/login",
            new SmartEstimatePlatform.Api.Models.DTOs.Auth.LoginRequest { Email = email, Password = password });

    private async Task<HttpClient> LoginClientAsync(string email)
    {
        var response = await LoginAsync(email, Password);
        response.EnsureSuccessStatusCode();
        var token = (await response.Content.ReadFromJsonAsync<SmartEstimatePlatform.Api.Models.DTOs.Auth.AuthResponse>())!.Token;
        var client = _logged.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }
}
