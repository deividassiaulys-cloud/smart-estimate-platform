using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs.Auth;
using SmartEstimatePlatform.Api.Models.DTOs.Team;

namespace SmartEstimatePlatform.Api.Tests.TestSupport;

/// <summary>
/// Runs the real API in-process against an in-memory SQLite database, so tests go through the real
/// token validation, routing and company scoping. Reuse it for every feature's cross-company tests.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string JwtKey = "integration-test-signing-key-0123456789";

    private readonly SqliteConnection _connection = SqliteTestDatabase.OpenConnection();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=unused-in-tests");
        builder.UseSetting("Jwt:Key", JwtKey);

        builder.ConfigureServices(services =>
        {
            // Replace the MySQL configuration with the shared in-memory SQLite connection.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        EnsureDatabaseCreated();
    }

    /// <summary>
    /// Registers a new company through the API and returns a client logged in as its Manager.
    /// </summary>
    public async Task<LoggedInCompany> RegisterCompanyAsync(string companyName, string email)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            CompanyName = companyName,
            FirstName = "Test",
            LastName = "Manager",
            Email = email,
            Password = "Secret123"
        });
        response.EnsureSuccessStatusCode();

        var registered = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered.Token);
        return new LoggedInCompany(client, registered.UserId, registered.CompanyId, registered.Token);
    }

    /// <summary>
    /// Posts a login and returns the raw response, so tests can check rejected logins too.
    /// </summary>
    public Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });

    /// <summary>
    /// Creates a Viewer in the given company and returns a client logged in as that Viewer.
    /// </summary>
    public async Task<HttpClient> CreateViewerClientAsync(LoggedInCompany company)
    {
        var created = await company.Client.PostAsJsonAsync("/api/team/viewers", new CreateViewerRequest
        {
            FirstName = "Ona",
            LastName = "Onaitė",
            Email = "ona@example.com",
            Password = "Viewer123"
        });
        created.EnsureSuccessStatusCode();

        var login = await LoginAsync("ona@example.com", "Viewer123");
        var token = (await login.Content.ReadFromJsonAsync<AuthResponse>())!.Token;
        return CreateClient(token);
    }

    /// <summary>
    /// Opens a database context that sees only the given company's records, as a request of that company would;
    /// null means nobody is logged in.
    /// </summary>
    public AppDbContext CreateDbContext(int? companyId)
    {
        CreateClient(); // Makes sure the database exists.
        var options = Services.GetRequiredService<DbContextOptions<AppDbContext>>();
        return new AppDbContext(options, new FixedCurrentUser(companyId));
    }

    /// <summary>
    /// Returns a client that sends the given token.
    /// </summary>
    public HttpClient CreateClient(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private bool _databaseCreated;

    private void EnsureDatabaseCreated()
    {
        if (_databaseCreated)
        {
            return;
        }

        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        _databaseCreated = true;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

/// <summary>
/// A registered company with an HTTP client logged in as its Manager.
/// </summary>
public sealed record LoggedInCompany(HttpClient Client, int ManagerId, int CompanyId, string Token);
