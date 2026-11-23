using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs.Auth;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services.Mapping;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Handles account registration, login with lockout after repeated failures, and looking up the logged-in user.
/// </summary>
public class AuthService(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    /// <summary>
    /// Failed attempts within <see cref="FailedLoginWindow"/> that lock the account.
    /// </summary>
    public const int MaxFailedLogins = 5;

    /// <summary>
    /// How long a streak of failed attempts counts, measured from its first failure.
    /// </summary>
    public static readonly TimeSpan FailedLoginWindow = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How long an account stays locked.
    /// </summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // A hash of a random password, made once, that an unknown email is checked against, so a login with an unknown
    // email costs one password verification like a known one and its response time does not reveal the account
    // (feature 021, F5). No password ever matches it.
    private static readonly User UnknownEmailUser = new(string.Empty, string.Empty, string.Empty, UserRole.Viewer, companyId: 0);

    private static readonly Lazy<string> UnknownEmailHash = new(() =>
        new PasswordHasher<User>().HashPassword(UnknownEmailUser, Guid.NewGuid().ToString()));

    /// <inheritdoc />
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var manager = new User(
            request.FirstName, request.LastName, request.Email, UserRole.Manager, new Company(request.CompanyName));

        await CreateAccountAsync(manager, request.Password);
        return manager.ToAuthResponse(tokenService.CreateToken(manager));
    }

    /// <inheritdoc />
    public Task CreateAccountAsync(User account, string password)
    {
        account.SetPasswordHash(passwordHasher.HashPassword(account, password));
        db.Users.Add(account);
        return UniqueSave.SaveAsync(
            db,
            () => EmailExistsAsync(account.Email),
            () => new DuplicateEmailException(account.Email));
    }

    /// <inheritdoc />
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Nobody is logged in yet, so the company scoping is bypassed to find the account (see plan 011).
        // The Email column's case-insensitive collation makes this comparison case-insensitive.
        var user = await db.Users.IgnoreQueryFilters()
            .SingleOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
        {
            passwordHasher.VerifyHashedPassword(UnknownEmailUser, UnknownEmailHash.Value, request.Password);
            logger.LogWarning("Failed login for an unknown email {EmailHash}", EmailHash.Of(request.Email));
            throw new InvalidCredentialsException();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (user.LockoutEnd > now)
        {
            logger.LogWarning("Login refused for locked user {UserId}", user.Id);
            throw new AccountLockedException(MinutesUntil(user.LockoutEnd.Value, now));
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            RecordFailedLogin(user, now);
            await db.SaveChangesAsync();

            if (user.LockoutEnd > now)
            {
                logger.LogWarning("Account locked: user {UserId} until {LockoutEnd:O}", user.Id, user.LockoutEnd);
                throw new AccountLockedException(MinutesUntil(user.LockoutEnd.Value, now));
            }

            logger.LogWarning("Failed login for user {UserId}", user.Id);
            throw new InvalidCredentialsException();
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.SetPasswordHash(passwordHasher.HashPassword(user, request.Password));
        }
        user.ResetLoginFailures();
        await db.SaveChangesAsync();

        return user.ToAuthResponse(tokenService.CreateToken(user));
    }

    /// <inheritdoc />
    public async Task<CurrentUserResponse> GetCurrentUserAsync()
    {
        var user = await db.Users.Include(u => u.Company)
                       .SingleOrDefaultAsync(u => u.Id == currentUser.UserId)
                   ?? throw new UnauthorizedAccessException("The logged-in user no longer exists.");
        return user.ToCurrentUserResponse();
    }

    /// <inheritdoc />
    public async Task LogoutAsync()
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == currentUser.UserId)
                   ?? throw new UnauthorizedAccessException("The logged-in user no longer exists.");
        user.InvalidateTokens();
        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public Task<bool> IsTokenUserValidAsync(int userId, int companyId, int tokenVersion) =>
        // Runs while the token is being validated, before the current company is known (see plan 011).
        db.Users.IgnoreQueryFilters().AnyAsync(
            u => u.Id == userId && u.CompanyId == companyId && u.TokenVersion == tokenVersion);

    // A streak runs from its first failure; once the window has passed without a lock, the count starts over.
    private static void RecordFailedLogin(User user, DateTime now)
    {
        var failedCount = user.FailedLoginCount;
        var firstFailedAt = user.FirstFailedLoginAt;
        if (firstFailedAt is null || now - firstFailedAt >= FailedLoginWindow)
        {
            failedCount = 0;
            firstFailedAt = now;
        }

        failedCount++;
        if (failedCount >= MaxFailedLogins)
        {
            user.SetLoginState(failedLoginCount: 0, firstFailedLoginAt: null, lockoutEnd: now + LockoutDuration);
        }
        else
        {
            user.SetLoginState(failedCount, firstFailedAt, user.LockoutEnd);
        }
    }

    private static int MinutesUntil(DateTime end, DateTime now) => (int)Math.Ceiling((end - now).TotalMinutes);

    // Emails are unique across all companies, so this check bypasses the company scoping (see plan 011).
    // The Email column's case-insensitive collation makes this comparison case-insensitive.
    private Task<bool> EmailExistsAsync(string email) =>
        db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email);
}
