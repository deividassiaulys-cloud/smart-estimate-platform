using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Tests.AuthServiceTests;

/// <summary>
/// Runs AuthService against a real relational database (in-memory SQLite), so the unique email index
/// and the case-insensitive email collation are exercised. The clock is controllable through <see cref="Clock"/>.
/// </summary>
public abstract class AuthServiceTestBase : IDisposable
{
    // Same name as the MySQL collation in AppDbContext; registered on SQLite with case-insensitive semantics.
    private const string EmailCollation = "utf8mb4_0900_as_ci";

    private readonly SqliteConnection _connection;

    protected readonly DbContextOptions<AppDbContext> Options;

    protected readonly TestTimeProvider Clock = new();

    protected static readonly JwtOptions Jwt = new()
    {
        Key = "test-signing-key-that-is-long-enough-0123456789",
        Issuer = "SmartEstimatePlatform",
        Audience = "SmartEstimatePlatform"
    };

    protected AuthServiceTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _connection.CreateCollation(EmailCollation,
            (x, y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase));

        Options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = new AppDbContext(Options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    /// <summary>
    /// Runs an action against a fresh AuthService and DbContext, as one request would.
    /// </summary>
    protected async Task<T> WithServiceAsync<T>(Func<AuthService, Task<T>> action)
    {
        await using var db = new AppDbContext(Options);
        var service = new AuthService(
            db,
            new PasswordHasher<User>(),
            new TokenService(Microsoft.Extensions.Options.Options.Create(Jwt)),
            Clock);
        return await action(service);
    }

    protected sealed class TestTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;

        public void Advance(TimeSpan by) => Now += by;
    }
}
