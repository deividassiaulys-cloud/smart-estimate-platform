using System.Net;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.AuthServiceTests;

/// <summary>
/// Logout through the real API: it ends the session on the server, not only in the browser (feature 021, F6).
/// </summary>
public sealed class LogoutEndpointTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task TokenUsedAfterLogout_IsRejected()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var copiedToken = _factory.CreateClient(company.Token);

        var logout = await company.Client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await copiedToken.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await company.Client.GetAsync("/api/projects")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _factory.LoginAsync("alpha@example.com", "Secret123")).StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutLogin_IsUnauthorized()
    {
        var response = await _factory.CreateClient().PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
