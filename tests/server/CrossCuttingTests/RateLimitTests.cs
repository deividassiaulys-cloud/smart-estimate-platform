using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartEstimatePlatform.Api.Models.DTOs.Auth;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CrossCuttingTests;

/// <summary>
/// Login and registration share a limit per client address (feature 021, F4). The limit is lowered to 3 requests
/// here; the default is 10 per 60 seconds, and ApiFactory raises it so other tests are not limited.
/// </summary>
public sealed class RateLimitTests : IDisposable
{
    private const int Limit = 3;

    private readonly ApiFactory _factory = new();
    private readonly WebApplicationFactory<Program> _limited;

    public RateLimitTests() =>
        _limited = _factory.WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:Auth:PermitLimit", Limit.ToString()));

    public void Dispose()
    {
        _limited.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Login_OverTheLimit_Returns429()
    {
        var client = _limited.CreateClient();
        var token = await RegisterAsync(client, "alpha@example.com");
        for (var i = 1; i < Limit; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, "Wrong1234")).StatusCode);
        }

        var limited = await LoginAsync(client, "Secret123");

        await AssertTooManyRequestsAsync(limited);
        // Only login and registration are limited: the rest of the API keeps answering.
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Register_OverTheLimit_Returns429()
    {
        var client = _limited.CreateClient();
        for (var i = 0; i < Limit; i++)
        {
            await RegisterAsync(client, $"user{i}@example.com");
        }

        var limited = await client.PostAsJsonAsync("/api/auth/register", Registration("late@example.com"));

        await AssertTooManyRequestsAsync(limited);
    }

    private static async Task AssertTooManyRequestsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(429, problem!.Status);
        Assert.True(response.Headers.RetryAfter is not null, "429 must tell the client when to retry.");
    }

    private static async Task<string> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", Registration(email));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!.Token;
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = "alpha@example.com", Password = password });

    private static RegisterRequest Registration(string email) => new()
    {
        CompanyName = "Alpha UAB",
        FirstName = "Test",
        LastName = "Manager",
        Email = email,
        Password = "Secret123"
    };
}
