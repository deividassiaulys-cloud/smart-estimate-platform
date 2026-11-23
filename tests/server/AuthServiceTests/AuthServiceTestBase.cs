using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.AuthServiceTests;

/// <summary>
/// Runs AuthService against a real relational database (in-memory SQLite), so the unique email index
/// and the case-insensitive email collation are exercised. The clock is controllable through <see cref="Clock"/>.
/// </summary>
public abstract class AuthServiceTestBase : IDisposable
{
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
        _connection = SqliteTestDatabase.OpenConnection();

        Options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = new AppDbContext(Options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    /// <summary>
    /// Runs an action against a fresh AuthService and DbContext, as one request would.
    /// <paramref name="companyId"/> is the logged-in user's company; null means nobody is logged in.
    /// <paramref name="passwordHasher"/> replaces the real hasher, e.g. to count its calls.
    /// </summary>
    protected async Task<T> WithServiceAsync<T>(
        Func<AuthService, Task<T>> action, int? companyId = null, int? userId = null, IPasswordHasher<User>? passwordHasher = null)
    {
        var currentUser = new FixedCurrentUser(companyId, userId);
        await using var db = new AppDbContext(Options, currentUser);
        var service = new AuthService(
            db,
            passwordHasher ?? new PasswordHasher<User>(),
            new TokenService(Microsoft.Extensions.Options.Options.Create(Jwt)),
            currentUser,
            Clock);
        return await action(service);
    }

    /// <summary>
    /// The real password hasher, counting how often a password is verified.
    /// </summary>
    protected sealed class CountingPasswordHasher : IPasswordHasher<User>
    {
        private readonly PasswordHasher<User> _inner = new();

        public int VerifyCalls { get; private set; }

        public string HashPassword(User user, string password) => _inner.HashPassword(user, password);

        public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
        {
            VerifyCalls++;
            return _inner.VerifyHashedPassword(user, hashedPassword, providedPassword);
        }
    }

    protected sealed class TestTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;

        public void Advance(TimeSpan by) => Now += by;
    }
}
