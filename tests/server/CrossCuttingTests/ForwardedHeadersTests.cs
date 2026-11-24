using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SmartEstimatePlatform.Api.Models.DTOs.Auth;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CrossCuttingTests;

/// <summary>
/// Behind Caddy the API trusts X-Forwarded-For and X-Forwarded-Proto only from the proxy's network (feature 022,
/// T358), so the login rate limit sees the real client and a forwarded HTTPS request counts as HTTPS. ApiFactory
/// trusts <see cref="ApiFactory.TrustedNetwork"/>; the login limit is lowered to 3 here.
/// </summary>
public sealed class ForwardedHeadersTests : IDisposable
{
    private const int Limit = 3;
    private const string Proxy = "10.99.0.5";
    private const string Outsider = "198.51.100.9";

    private readonly ApiFactory _factory = new();
    private readonly WebApplicationFactory<Program> _behindProxy;

    public ForwardedHeadersTests() =>
        _behindProxy = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Auth:PermitLimit", Limit.ToString());
            builder.ConfigureServices(services => services.AddTransient<IStartupFilter, TestRemoteAddress>());
        });

    public void Dispose()
    {
        _behindProxy.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task TrustedProxy_RateLimitUsesTheForwardedAddress()
    {
        for (var i = 0; i < Limit; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(from: Proxy, client: "203.0.113.7")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await LoginAsync(from: Proxy, client: "203.0.113.7")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(from: Proxy, client: "203.0.113.8")).StatusCode);
    }

    [Fact]
    public async Task UntrustedSource_ForwardedHeadersAreIgnored()
    {
        for (var i = 0; i < Limit; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(from: Outsider, client: $"203.0.113.{10 + i}")).StatusCode);
        }

        // Another claimed client address does not help: the limit still counts the real sender.
        Assert.Equal(HttpStatusCode.TooManyRequests, (await LoginAsync(from: Outsider, client: "203.0.113.99")).StatusCode);
    }

    [Fact]
    public async Task ForwardedHttps_IsNotRedirectedAndGetsHsts()
    {
        var client = _behindProxy.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://smart-estimate.test"),
            AllowAutoRedirect = false
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/units");
        request.Headers.Add(TestRemoteAddress.Header, Proxy);
        request.Headers.Add("X-Forwarded-For", "203.0.113.7");
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.Headers.Contains("Strict-Transport-Security"), "A forwarded HTTPS request must get HSTS.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-network")]
    public void MissingTrustedNetwork_StopsTheAppOutsideDevelopment(string trustedNetwork)
    {
        using var unconfigured = _factory.WithWebHostBuilder(builder => builder.UseSetting("ReverseProxy:TrustedNetwork", trustedNetwork));

        var error = Assert.Throws<InvalidOperationException>(() => unconfigured.CreateClient());

        Assert.Contains("ReverseProxy:TrustedNetwork", error.Message);
    }

    [Fact]
    public void MissingTrustedNetwork_IsAllowedInDevelopment()
    {
        using var development = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ReverseProxy:TrustedNetwork", "");
        });

        Assert.NotNull(development.CreateClient());
    }

    private async Task<HttpResponseMessage> LoginAsync(string from, string client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest { Email = "nobody@example.com", Password = "Wrong1234" })
        };
        request.Headers.Add(TestRemoteAddress.Header, from);
        request.Headers.Add("X-Forwarded-For", client);
        return await _behindProxy.CreateClient().SendAsync(request);
    }
}
