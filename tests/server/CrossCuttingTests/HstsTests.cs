using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CrossCuttingTests;

/// <summary>
/// Outside development the API tells browsers to keep using HTTPS (NFR-001). A non-local host is used,
/// because HSTS is never sent to localhost.
/// </summary>
public sealed class HstsTests : IDisposable
{
    private static readonly Uri SecureAddress = new("https://smart-estimate.test");

    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task OutsideDevelopment_ResponseHasStrictTransportSecurity()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = SecureAddress });

        var response = await client.GetAsync("/api/units");

        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task InDevelopment_ResponseHasNoStrictTransportSecurity()
    {
        using var development = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        var client = development.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = SecureAddress });

        var response = await client.GetAsync("/api/units");

        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }
}
