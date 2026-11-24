using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using SmartEstimatePlatform.Api.Models.DTOs.Auth;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CrossCuttingTests;

/// <summary>
/// Outside Development the console log is JSON, one entry per line, with the request's scopes (TraceId, RequestPath),
/// and no entry ever holds the signing key, the connection string, a password or a token (feature 022, T360).
/// </summary>
public sealed class ProductionLoggingTests : IDisposable
{
    private const string ConnectionString = "Server=db.internal;Database=smart_estimate;User=app;Password=connection-secret-42";
    private const string Password = "Secret123";

    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public void OutsideDevelopment_ConsoleIsJsonWithScopes()
    {
        var services = _factory.Services;

        Assert.Equal(ConsoleFormatterNames.Json, services.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue.FormatterName);
        var json = services.GetRequiredService<IOptionsMonitor<JsonConsoleFormatterOptions>>().CurrentValue;
        Assert.True(json.IncludeScopes);
        Assert.True(json.UseUtcTimestamp);
    }

    [Fact]
    public void InDevelopment_ConsoleIsNotJson()
    {
        using var development = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));

        var formatter = development.Services.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue.FormatterName;

        Assert.NotEqual(ConsoleFormatterNames.Json, formatter);
    }

    [Fact]
    public async Task Requests_LogNoKeyOrConnectionString()
    {
        var logs = new CapturingLoggerProvider();
        using var logged = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
            builder.ConfigureLogging(logging => logging.AddProvider(logs));
        });
        var client = logged.CreateClient();

        var registered = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            CompanyName = "Alpha UAB",
            FirstName = "Test",
            LastName = "Manager",
            Email = "alpha@example.com",
            Password = Password
        });
        var token = (await registered.Content.ReadFromJsonAsync<AuthResponse>())!.Token;
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = "alpha@example.com", Password = Password });
        await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = "alpha@example.com", Password = "Wrong-Password-1" });
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        await client.GetAsync("/api/materials/999999");
        var loginToken = (await login.Content.ReadFromJsonAsync<AuthResponse>())!.Token;

        Assert.NotEmpty(logs.Entries);
        var everything = string.Join('\n', logs.Entries.Select(e =>
            string.Join(' ', [e.Message, .. e.Values.Values, .. e.Scopes.Values])));
        foreach (var secret in new[] { ApiFactory.JwtKey, ConnectionString, "connection-secret-42", Password, "Wrong-Password-1", token, loginToken })
        {
            Assert.DoesNotContain(secret, everything);
        }
    }
}
