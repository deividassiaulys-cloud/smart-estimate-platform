using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Tests.AuthServiceTests;

/// <summary>
/// Integration tests for login, the lockout after repeated failures, and the logged-in user lookups.
/// </summary>
public sealed class LoginAsyncTests : AuthServiceTestBase
{
    private const string Email = "jonas@example.com";
    private const string Password = "Secret123";

    private int _userId;
    private int _companyId;

    public LoginAsyncTests()
    {
        var manager = WithServiceAsync(service => service.RegisterAsync(new RegisterRequest
        {
            CompanyName = "Statyba UAB",
            FirstName = "Jonas",
            LastName = "Jonaitis",
            Email = Email,
            Password = Password
        })).GetAwaiter().GetResult();
        _userId = manager.Id;
        _companyId = manager.CompanyId;
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        var response = await LoginAsync(Email, Password);

        Assert.False(string.IsNullOrEmpty(response.Token));
        Assert.Equal(_userId, response.UserId);
        Assert.Equal(UserRole.Manager, response.Role);
    }

    [Fact]
    public async Task LoginAsync_EmailInDifferentCase_Succeeds()
    {
        var response = await LoginAsync("JONAS@Example.COM", Password);

        Assert.Equal(_userId, response.UserId);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmailAndWrongPassword_GetTheSameError()
    {
        var unknownEmail = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => LoginAsync("nobody@example.com", Password));
        var wrongPassword = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => LoginAsync(Email, "Wrong1234"));

        Assert.Equal(unknownEmail.Message, wrongPassword.Message);
    }

    [Fact]
    public async Task LoginAsync_FiveFailuresWithin10Minutes_LocksAccount()
    {
        await FailAsync(times: 4, gap: TimeSpan.FromMinutes(2));

        Clock.Advance(TimeSpan.FromMinutes(1));
        var locked = await Assert.ThrowsAsync<AccountLockedException>(() => LoginAsync(Email, "Wrong1234"));

        Assert.Equal(15, locked.MinutesRemaining);
    }

    [Fact]
    public async Task LoginAsync_WhileLocked_CorrectPasswordIsRejectedWithMinutesRemaining()
    {
        await LockAccountAsync();

        Clock.Advance(TimeSpan.FromMinutes(5).Add(TimeSpan.FromSeconds(30)));
        var locked = await Assert.ThrowsAsync<AccountLockedException>(() => LoginAsync(Email, Password));

        Assert.Equal(10, locked.MinutesRemaining);
    }

    [Fact]
    public async Task LoginAsync_After15Minutes_WorksAgain()
    {
        await LockAccountAsync();

        Clock.Advance(TimeSpan.FromMinutes(15));
        var response = await LoginAsync(Email, Password);

        Assert.Equal(_userId, response.UserId);
    }

    [Fact]
    public async Task LoginAsync_StreakOlderThan10Minutes_StartsOver()
    {
        await FailAsync(times: 4, gap: TimeSpan.FromMinutes(1));

        // 10 minutes after the first failure the streak expires, so this failure starts a new one.
        Clock.Advance(TimeSpan.FromMinutes(7));
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => LoginAsync(Email, "Wrong1234"));

        await FailAsync(times: 3, gap: TimeSpan.FromMinutes(1));
        Assert.False(await IsLockedAsync());
    }

    [Fact]
    public async Task LoginAsync_SuccessfulLogin_ResetsStreak()
    {
        await FailAsync(times: 4, gap: TimeSpan.Zero);
        await LoginAsync(Email, Password);

        await FailAsync(times: 4, gap: TimeSpan.Zero);

        Assert.False(await IsLockedAsync());
    }

    [Fact]
    public async Task GetUserAsync_ReturnsNameCompanyAndRole()
    {
        var user = await WithServiceAsync(service => service.GetUserAsync(_userId), _companyId);

        Assert.NotNull(user);
        Assert.Equal("Jonas", user.FirstName);
        Assert.Equal("Jonaitis", user.LastName);
        Assert.Equal("Statyba UAB", user.Company.Name);
        Assert.Equal(UserRole.Manager, user.Role);
    }

    [Fact]
    public async Task IsTokenUserValidAsync_RemovedUser_IsRejected()
    {
        Assert.True(await WithServiceAsync(service => service.IsTokenUserValidAsync(_userId, _companyId, tokenVersion: 0)));

        await using (var db = new AppDbContext(Options))
        {
            await db.Users.IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
        }

        Assert.False(await WithServiceAsync(service => service.IsTokenUserValidAsync(_userId, _companyId, tokenVersion: 0)));
    }

    [Fact]
    public async Task IsTokenUserValidAsync_CompanyDoesNotMatchUser_IsRejected()
    {
        Assert.False(await WithServiceAsync(service => service.IsTokenUserValidAsync(_userId, _companyId + 1, tokenVersion: 0)));
    }

    private Task<LoginResponse> LoginAsync(string email, string password) =>
        WithServiceAsync(service => service.LoginAsync(new LoginRequest { Email = email, Password = password }));

    private async Task FailAsync(int times, TimeSpan gap)
    {
        for (var i = 0; i < times; i++)
        {
            if (i > 0)
            {
                Clock.Advance(gap);
            }
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => LoginAsync(Email, "Wrong1234"));
        }
    }

    private async Task LockAccountAsync()
    {
        await FailAsync(times: 4, gap: TimeSpan.Zero);
        await Assert.ThrowsAsync<AccountLockedException>(() => LoginAsync(Email, "Wrong1234"));
    }

    private async Task<bool> IsLockedAsync()
    {
        await using var db = new AppDbContext(Options);
        var user = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == _userId);
        return user.LockoutEnd > Clock.GetUtcNow().UtcDateTime;
    }
}
