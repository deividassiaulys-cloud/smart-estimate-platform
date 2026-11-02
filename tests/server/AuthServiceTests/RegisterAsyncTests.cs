using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs.Auth;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Tests.AuthServiceTests;

/// <summary>
/// Integration tests for registration.
/// </summary>
public sealed class RegisterAsyncTests : AuthServiceTestBase
{
    [Fact]
    public async Task RegisterAsync_ValidData_CreatesCompanyAndManager()
    {
        var manager = await RegisterAsync(ValidRequest());

        await using var db = new AppDbContext(Options);
        var saved = await db.Users.IgnoreQueryFilters().Include(u => u.Company).SingleAsync();
        Assert.Equal(manager.UserId, saved.Id);
        Assert.Equal(UserRole.Manager, saved.Role);
        Assert.Equal("Statyba UAB", saved.Company.Name);
        Assert.NotEqual("Secret123", saved.PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_IsRejectedAndCreatesNothing()
    {
        await RegisterAsync(ValidRequest());

        var duplicate = ValidRequest() with { CompanyName = "Kita UAB" };
        await Assert.ThrowsAsync<DuplicateEmailException>(() => RegisterAsync(duplicate));

        await AssertCountsAsync(companies: 1, users: 1);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmailDifferentCase_IsRejectedAndCreatesNothing()
    {
        await RegisterAsync(ValidRequest());

        var duplicate = ValidRequest() with { Email = "JONAS@Example.COM" };
        await Assert.ThrowsAsync<DuplicateEmailException>(() => RegisterAsync(duplicate));

        await AssertCountsAsync(companies: 1, users: 1);
    }

    [Fact]
    public async Task RegisterAsync_PasswordExactly8Characters_IsAccepted()
    {
        var request = ValidRequest() with { Password = "Abc12345" };

        await RegisterAsync(request);

        await AssertCountsAsync(companies: 1, users: 1);
    }

    private Task<AuthResponse> RegisterAsync(RegisterRequest request) =>
        WithServiceAsync(service => service.RegisterAsync(request));

    private async Task AssertCountsAsync(int companies, int users)
    {
        await using var db = new AppDbContext(Options);
        Assert.Equal(companies, await db.Companies.CountAsync());
        Assert.Equal(users, await db.Users.IgnoreQueryFilters().CountAsync());
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
