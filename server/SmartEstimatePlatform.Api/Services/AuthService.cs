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
    TimeProvider timeProvider) : IAuthService
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

    /// <inheritdoc />
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var manager = new User(
            request.FirstName, request.LastName, request.Email, UserRole.Manager, new Company(request.CompanyName));

        await CreateAccountAsync(manager, request.Password);
        return manager.ToAuthResponse(tokenService.CreateToken(manager));
    }

    /// <inheritdoc />
    public async Task CreateAccountAsync(User account, string password)
    {
        if (await EmailExistsAsync(account.Email))
        {
            throw new DuplicateEmailException(account.Email);
        }

        account.SetPasswordHash(passwordHasher.HashPassword(account, password));
        db.Users.Add(account);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            // A concurrent request with the same email won the race; the unique index rejected this one.
            if (await EmailExistsAsync(account.Email))
            {
                throw new DuplicateEmailException(account.Email);
            }

            throw;
        }
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
            throw new InvalidCredentialsException();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (user.LockoutEnd > now)
        {
            throw new AccountLockedException(MinutesUntil(user.LockoutEnd.Value, now));
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            RecordFailedLogin(user, now);
            await db.SaveChangesAsync();

            if (user.LockoutEnd > now)
            {
                throw new AccountLockedException(MinutesUntil(user.LockoutEnd.Value, now));
            }
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
