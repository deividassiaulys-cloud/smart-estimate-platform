using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.AuthServiceTests;

/// <summary>
/// Proves through the real API that passwords are stored only as hashes, using the password hasher the
/// application registers (feature 021, T019: it comes from the shared framework, not a separate package).
/// </summary>
public sealed class PasswordHashingTests : IDisposable
{
    private const string Password = "Secret123";

    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task StoredHash_IsNotThePlainPassword()
    {
        await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var user = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == "alpha@example.com");

        Assert.False(string.IsNullOrEmpty(user.PasswordHash));
        Assert.DoesNotContain(Password, user.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, user.PasswordHash, Password));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, user.PasswordHash, "Wrong1234"));
    }
}
