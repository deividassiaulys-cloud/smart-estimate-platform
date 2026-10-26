using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.TeamServiceTests;

/// <summary>
/// Integration tests for Viewer account management, through the real API.
/// </summary>
public sealed class TeamEndpointsTests : IDisposable
{
    private const string ViewerEmail = "ona@example.com";
    private const string ViewerPassword = "Viewer123";

    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task CreateViewer_AddsAViewerToTheManagersCompany()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var response = await CreateViewerAsync(company);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var team = await company.Client.GetFromJsonAsync<List<TeamMemberResponse>>("/api/team");
        Assert.Collection(team!,
            manager => Assert.Equal(UserRole.Manager, manager.Role),
            viewer =>
            {
                Assert.Equal(ViewerEmail, viewer.Email);
                Assert.Equal(UserRole.Viewer, viewer.Role);
            });

        var login = await _factory.LoginAsync(ViewerEmail, ViewerPassword);
        var loggedIn = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal(company.CompanyId, loggedIn!.CompanyId);
        Assert.Equal(UserRole.Viewer, loggedIn.Role);
    }

    [Theory]
    [InlineData("ONA@Example.com")]
    [InlineData("beta@example.com")]
    public async Task CreateViewer_EmailAlreadyInUseAnywhere_IsRejected(string email)
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        await CreateViewerAsync(company);

        var response = await CreateViewerAsync(company, email);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(2, (await company.Client.GetFromJsonAsync<List<TeamMemberResponse>>("/api/team"))!.Count);
    }

    [Fact]
    public async Task CreateViewer_ManagersOwnEmail_IsRejected()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var response = await CreateViewerAsync(company, "alpha@example.com");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("Short1")]
    [InlineData(ViewerEmail)]
    public async Task CreateViewer_WeakPassword_IsRejected(string password)
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var response = await CreateViewerAsync(company, password: password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single((await company.Client.GetFromJsonAsync<List<TeamMemberResponse>>("/api/team"))!);
    }

    [Fact]
    public async Task ResetPassword_ViewerLogsInWithTheNewPasswordOnly()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var viewerId = await CreateViewerIdAsync(company);

        var response = await ResetPasswordAsync(company, viewerId, "NewSecret456");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _factory.LoginAsync(ViewerEmail, "NewSecret456")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.LoginAsync(ViewerEmail, ViewerPassword)).StatusCode);
    }

    [Theory]
    [InlineData("Short1")]
    [InlineData(ViewerEmail)]
    public async Task ResetPassword_WeakPassword_IsRejectedAndOldPasswordStillWorks(string password)
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var viewerId = await CreateViewerIdAsync(company);

        var response = await ResetPasswordAsync(company, viewerId, password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _factory.LoginAsync(ViewerEmail, ViewerPassword)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_RejectsTokensIssuedBeforeIt()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var viewerId = await CreateViewerIdAsync(company);
        var oldToken = await LoginViewerAsync(ViewerPassword);
        Assert.Equal(HttpStatusCode.OK, (await _factory.CreateClient(oldToken).GetAsync("/api/auth/me")).StatusCode);

        await ResetPasswordAsync(company, viewerId, "NewSecret456");

        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient(oldToken).GetAsync("/api/auth/me")).StatusCode);
        var newToken = await LoginViewerAsync("NewSecret456");
        Assert.Equal(HttpStatusCode.OK, (await _factory.CreateClient(newToken).GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task RemoveViewer_CannotLogInAndTheirTokenIsRejected()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var viewerId = await CreateViewerIdAsync(company);
        var viewerToken = await LoginViewerAsync(ViewerPassword);

        var response = await company.Client.DeleteAsync($"/api/team/viewers/{viewerId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.LoginAsync(ViewerEmail, ViewerPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient(viewerToken).GetAsync("/api/auth/me")).StatusCode);
        Assert.Single((await company.Client.GetFromJsonAsync<List<TeamMemberResponse>>("/api/team"))!);
    }

    [Fact]
    public async Task RemoveViewer_FreesTheEmailForANewAccount()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var viewerId = await CreateViewerIdAsync(company);
        await company.Client.DeleteAsync($"/api/team/viewers/{viewerId}");

        var response = await CreateViewerAsync(company);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Manager_CannotBeRemovedOrHavePasswordReset()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var remove = await company.Client.DeleteAsync($"/api/team/viewers/{company.ManagerId}");
        var reset = await ResetPasswordAsync(company, company.ManagerId, "NewSecret456");

        Assert.Equal(HttpStatusCode.NotFound, remove.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, reset.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _factory.LoginAsync("alpha@example.com", "Secret123")).StatusCode);
    }

    [Fact]
    public async Task Viewer_CannotUseTeamEndpoints()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var viewerId = await CreateViewerIdAsync(company);
        var viewer = _factory.CreateClient(await LoginViewerAsync(ViewerPassword));

        var list = await viewer.GetAsync("/api/team");
        var create = await viewer.PostAsJsonAsync("/api/team/viewers", NewViewer("other@example.com", ViewerPassword));
        var remove = await viewer.DeleteAsync($"/api/team/viewers/{viewerId}");
        var reset = await viewer.PatchAsJsonAsync($"/api/team/viewers/{viewerId}/password",
            new ResetPasswordRequest { NewPassword = "NewSecret456" });

        Assert.All([list, create, remove, reset], r => Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode));
    }

    [Fact]
    public async Task TeamEndpoints_WithoutLogin_AreUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/team");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static CreateViewerRequest NewViewer(string email, string password) => new()
    {
        FirstName = "Ona",
        LastName = "Onaitė",
        Email = email,
        Password = password
    };

    private Task<HttpResponseMessage> CreateViewerAsync(
        LoggedInCompany company, string email = ViewerEmail, string password = ViewerPassword) =>
        company.Client.PostAsJsonAsync("/api/team/viewers", NewViewer(email, password));

    private async Task<int> CreateViewerIdAsync(LoggedInCompany company)
    {
        var response = await CreateViewerAsync(company);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TeamMemberResponse>())!.Id;
    }

    private static Task<HttpResponseMessage> ResetPasswordAsync(LoggedInCompany company, int viewerId, string password) =>
        company.Client.PatchAsJsonAsync($"/api/team/viewers/{viewerId}/password",
            new ResetPasswordRequest { NewPassword = password });

    private async Task<string> LoginViewerAsync(string password)
    {
        var response = await _factory.LoginAsync(ViewerEmail, password);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!.Token;
    }
}
