using System.Net;
using Microsoft.AspNetCore.Hosting;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CrossCuttingTests;

/// <summary>
/// Outside Development only the hosts in AllowedHosts are served, and "*" stops the app (feature 021, F7).
/// ApiFactory lists the test hosts (localhost, smart-estimate.test).
/// </summary>
public sealed class HostFilteringTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task UnknownHost_IsRejectedOutsideDevelopment()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/units");
        request.Headers.Host = "attacker.example";

        var unknown = await client.SendAsync(request);
        var known = await client.GetAsync("/api/units");

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, known.StatusCode);
    }

    [Fact]
    public void WildcardHosts_StopTheAppOutsideDevelopment()
    {
        using var wildcard = _factory.WithWebHostBuilder(builder => builder.UseSetting("AllowedHosts", "*"));

        var error = Assert.Throws<InvalidOperationException>(() => wildcard.CreateClient());

        Assert.Contains("AllowedHosts", error.Message);
    }

    [Fact]
    public void WildcardHosts_AreAllowedInDevelopment()
    {
        using var development = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("AllowedHosts", "*");
        });

        Assert.NotNull(development.CreateClient());
    }
}
