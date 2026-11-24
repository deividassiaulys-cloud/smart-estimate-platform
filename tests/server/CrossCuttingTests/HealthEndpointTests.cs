using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CrossCuttingTests;

/// <summary>
/// The deployment's health endpoints (feature 022, T357): /health says the API process runs and which version it is,
/// without a token and without the database; /health/ready says whether the database answers.
/// </summary>
public sealed class HealthEndpointTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Health_WithoutToken_ReturnsStatusAndVersion()
    {
        using var versioned = _factory.WithWebHostBuilder(builder => builder.UseSetting("APP_VERSION", "3f2a9c1"));

        var response = await versioned.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthBody>();
        Assert.Equal(new HealthBody("Healthy", "3f2a9c1"), body);
    }

    [Fact]
    public async Task Health_WithoutConfiguredVersion_ReportsDev()
    {
        var body = await _factory.CreateClient().GetFromJsonAsync<HealthBody>("/health");

        Assert.Equal("dev", body!.Version);
    }

    [Fact]
    public async Task Ready_WithDatabase_Returns200()
    {
        var response = await _factory.CreateClient().GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Ready_WithBrokenDatabase_Returns503WithoutDetails()
    {
        using var broken = WithUnreachableDatabase();

        var response = await broken.CreateClient().GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_WithBrokenDatabase_StillReturns200()
    {
        using var broken = WithUnreachableDatabase();

        var response = await broken.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// The API with a database file that does not exist and may not be created, so no connection can be made.
    /// </summary>
    private WebApplicationFactory<Program> WithUnreachableDatabase() =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite("Data Source=/nonexistent-directory/smart-estimate.db;Mode=ReadOnly"));
        }));

    private sealed record HealthBody(string Status, string Version);
}
