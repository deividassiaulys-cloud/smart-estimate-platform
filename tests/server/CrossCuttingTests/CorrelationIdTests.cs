using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CrossCuttingTests;

/// <summary>
/// Every response names its correlation id (the request's trace id) in X-Correlation-ID, and the log entries written
/// for that request carry the same id, so one failed request can be found in the logs (feature 022, T359).
/// </summary>
public sealed class CorrelationIdTests : IDisposable
{
    private const string Header = "X-Correlation-ID";

    private readonly ApiFactory _factory = new();
    private readonly CapturingLoggerProvider _logs = new();
    private readonly WebApplicationFactory<Program> _logged;

    public CorrelationIdTests() =>
        _logged = _factory.WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.AddProvider(_logs)));

    public void Dispose()
    {
        _logged.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task EveryResponse_HasACorrelationId()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var ok = await _factory.CreateClient().GetAsync("/health");
        var unauthorized = await _factory.CreateClient().GetAsync("/api/units");
        var notFound = await company.Client.GetAsync("/api/materials/999999");

        Assert.Equal((HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound),
            (ok.StatusCode, unauthorized.StatusCode, notFound.StatusCode));
        var ids = new[] { ok, unauthorized, notFound }.Select(CorrelationId).ToList();
        Assert.All(ids, id => Assert.Matches("^[0-9a-f]{32}$", id));
        Assert.Equal(3, ids.Distinct().Count());
    }

    [Fact]
    public async Task ProblemDetailsTraceId_ContainsTheCorrelationId()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var response = await company.Client.GetAsync("/api/materials/999999");

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(CorrelationId(response), problem.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task LogEntriesOfARequest_CarryTheCorrelationId()
    {
        var token = await RegisterAsync();
        var client = _logged.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.GetAsync("/api/materials/999999");

        var notFound = Assert.Single(_logs.Entries, e => e.Level == LogLevel.Warning && e.Message.StartsWith("Not found"));
        Assert.Equal(CorrelationId(response), notFound.Scopes.GetValueOrDefault("TraceId"));
    }

    private async Task<string> RegisterAsync()
    {
        var response = await _logged.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            CompanyName = "Alpha UAB",
            FirstName = "Test",
            LastName = "Manager",
            Email = "alpha@example.com",
            Password = "Secret123"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    private static string CorrelationId(HttpResponseMessage response) =>
        Assert.Single(response.Headers.GetValues(Header));
}
