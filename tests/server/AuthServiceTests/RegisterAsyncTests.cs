using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Tests.AuthServiceTests;

/// <summary>
/// Integration tests for registration against a real relational database (in-memory SQLite),
/// so the unique email index and the case-insensitive email collation are exercised.
/// </summary>
public sealed class RegisterAsyncTests : IDisposable
{
    // Same name as the MySQL collation in AppDbContext; registered on SQLite with case-insensitive semantics.
    private const string EmailCollation = "utf8mb4_0900_as_ci";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public RegisterAsyncTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _connection.CreateCollation(EmailCollation,
            (x, y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase));

        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task RegisterAsync_ValidData_CreatesCompanyAndManager()
    {
        var manager = await RegisterAsync(ValidRequest());

        await using var db = new AppDbContext(_options);
        var saved = await db.Users.Include(u => u.Company).SingleAsync();
        Assert.Equal(manager.Id, saved.Id);
        Assert.Equal(UserRole.Manager, saved.Role);
        Assert.Equal("Statyba UAB", saved.Company.Name);
        Assert.NotEqual("Secret123", saved.PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_IsRejectedAndCreatesNothing()
    {
        await RegisterAsync(ValidRequest());

        var duplicate = ValidRequest();
        duplicate.CompanyName = "Kita UAB";
        await Assert.ThrowsAsync<DuplicateEmailException>(() => RegisterAsync(duplicate));

        await AssertCountsAsync(companies: 1, users: 1);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmailDifferentCase_IsRejectedAndCreatesNothing()
    {
        await RegisterAsync(ValidRequest());

        var duplicate = ValidRequest();
        duplicate.Email = "JONAS@Example.COM";
        await Assert.ThrowsAsync<DuplicateEmailException>(() => RegisterAsync(duplicate));

        await AssertCountsAsync(companies: 1, users: 1);
    }

    [Fact]
    public async Task RegisterAsync_PasswordShorterThan8Characters_IsRejectedAndCreatesNothing()
    {
        var request = ValidRequest();
        request.Password = "Abc1234";

        await Assert.ThrowsAsync<WeakPasswordException>(() => RegisterAsync(request));

        await AssertCountsAsync(companies: 0, users: 0);
    }

    [Fact]
    public async Task RegisterAsync_PasswordExactly8Characters_IsAccepted()
    {
        var request = ValidRequest();
        request.Password = "Abc12345";

        await RegisterAsync(request);

        await AssertCountsAsync(companies: 1, users: 1);
    }

    [Theory]
    [InlineData("jonas@example.com")]
    [InlineData("Jonas@Example.com")]
    public async Task RegisterAsync_PasswordEqualToEmail_IsRejectedAndCreatesNothing(string password)
    {
        var request = ValidRequest();
        request.Password = password;

        await Assert.ThrowsAsync<WeakPasswordException>(() => RegisterAsync(request));

        await AssertCountsAsync(companies: 0, users: 0);
    }

    private async Task<User> RegisterAsync(RegisterRequest request)
    {
        await using var db = new AppDbContext(_options);
        var service = new AuthService(db, new PasswordHasher<User>());
        return await service.RegisterAsync(request);
    }

    private async Task AssertCountsAsync(int companies, int users)
    {
        await using var db = new AppDbContext(_options);
        Assert.Equal(companies, await db.Companies.CountAsync());
        Assert.Equal(users, await db.Users.CountAsync());
    }

    private static RegisterRequest ValidRequest() => new()
    {
        CompanyName = "Statyba UAB",
        FirstName = "Jonas",
        LastName = "Jonaitis",
        Email = "jonas@example.com",
        Password = "Secret123"
    };
}
