using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Handles account registration, login with lockout after repeated failures, and looking up the logged-in user.
/// </summary>
public class AuthService(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    TokenService tokenService,
    TimeProvider timeProvider)
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

    /// <summary>
    /// Registers a visitor: creates their company and makes them its Manager.
    /// The company and the account are saved together, so either both exist or neither does.
    /// </summary>
    /// <returns>The created Manager account, with its company.</returns>
    /// <exception cref="WeakPasswordException">
    /// The password breaks the <see cref="PasswordRules"/>. Nothing is created.
    /// </exception>
    /// <exception cref="DuplicateEmailException">
    /// The email is already in use, compared case-insensitively. Nothing is created.
    /// </exception>
    public async Task<User> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var manager = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Role = UserRole.Manager,
            Company = new Company { Name = request.CompanyName }
        };

        await CreateAccountAsync(manager, request.Password, cancellationToken);
        return manager;
    }

    /// <summary>
    /// Saves a new account with a hashed password, after checking the <see cref="PasswordRules"/> and that
    /// the email is not used by any account in any company. Used for registration and for new Viewers.
    /// </summary>
    /// <exception cref="WeakPasswordException">The password breaks a rule. Nothing is saved.</exception>
    /// <exception cref="DuplicateEmailException">The email is already in use, compared case-insensitively. Nothing is saved.</exception>
    public async Task CreateAccountAsync(User account, string password, CancellationToken cancellationToken = default)
    {
        PasswordRules.EnsureStrong(password, account.Email);

        if (await EmailExistsAsync(account.Email, cancellationToken))
        {
            throw new DuplicateEmailException(account.Email);
        }

        account.PasswordHash = passwordHasher.HashPassword(account, password);
        db.Users.Add(account);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            // A concurrent request with the same email won the race; the unique index rejected this one.
            if (await EmailExistsAsync(account.Email, cancellationToken))
            {
                throw new DuplicateEmailException(account.Email);
            }

            throw;
        }
    }

    /// <summary>
    /// Logs a user in and issues their JWT. A successful login clears any failed-attempt streak.
    /// </summary>
    /// <exception cref="InvalidCredentialsException">
    /// The email is unknown or the password is wrong; the same error is used for both.
    /// A wrong password counts towards the lockout.
    /// </exception>
    /// <exception cref="AccountLockedException">
    /// The account is locked, or this failure was the one that locked it. Raised even when the password is correct.
    /// </exception>
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Nobody is logged in yet, so the company scoping is bypassed to find the account (see plan 011).
        // The Email column's case-insensitive collation makes this comparison case-insensitive.
        var user = await db.Users.IgnoreQueryFilters()
            .SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
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
            await db.SaveChangesAsync(cancellationToken);

            if (user.LockoutEnd > now)
            {
                throw new AccountLockedException(MinutesUntil(user.LockoutEnd.Value, now));
            }
            throw new InvalidCredentialsException();
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        }
        ClearFailedLogins(user);
        await db.SaveChangesAsync(cancellationToken);

        return new LoginResponse
        {
            Token = tokenService.CreateToken(user),
            UserId = user.Id,
            CompanyId = user.CompanyId,
            Role = user.Role
        };
    }

    /// <summary>
    /// Returns the user with their company, or null if the account does not exist in the current company.
    /// </summary>
    public Task<User?> GetUserAsync(int userId, CancellationToken cancellationToken = default) =>
        db.Users.Include(u => u.Company).SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

    /// <summary>
    /// Whether a token is still valid for its user: the user still exists, still belongs to the token's
    /// company, and their password has not been reset since the token was issued.
    /// </summary>
    public Task<bool> IsTokenUserValidAsync(
        int userId, int companyId, int tokenVersion, CancellationToken cancellationToken = default) =>
        // Runs while the token is being validated, before the current company is known (see plan 011).
        db.Users.IgnoreQueryFilters().AnyAsync(
            u => u.Id == userId && u.CompanyId == companyId && u.TokenVersion == tokenVersion,
            cancellationToken);

    // A streak runs from its first failure; once the window has passed without a lock, the count starts over.
    private static void RecordFailedLogin(User user, DateTime now)
    {
        if (user.FirstFailedLoginAt is null || now - user.FirstFailedLoginAt >= FailedLoginWindow)
        {
            user.FailedLoginCount = 0;
            user.FirstFailedLoginAt = now;
        }

        user.FailedLoginCount++;
        if (user.FailedLoginCount >= MaxFailedLogins)
        {
            user.LockoutEnd = now + LockoutDuration;
            user.FailedLoginCount = 0;
            user.FirstFailedLoginAt = null;
        }
    }

    private static void ClearFailedLogins(User user)
    {
        user.FailedLoginCount = 0;
        user.FirstFailedLoginAt = null;
        user.LockoutEnd = null;
    }

    private static int MinutesUntil(DateTime end, DateTime now) => (int)Math.Ceiling((end - now).TotalMinutes);

    // Emails are unique across all companies, so this check bypasses the company scoping (see plan 011).
    // The Email column's case-insensitive collation makes this comparison case-insensitive.
    private Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, cancellationToken);
}
