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
    /// Shortest password accepted at registration.
    /// </summary>
    public const int MinPasswordLength = 8;

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
    /// The password is shorter than <see cref="MinPasswordLength"/> characters or equals the email. Nothing is created.
    /// </exception>
    /// <exception cref="DuplicateEmailException">
    /// The email is already in use, compared case-insensitively. Nothing is created.
    /// </exception>
    public async Task<User> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        EnsurePasswordIsStrong(request.Password, request.Email);

        if (await EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new DuplicateEmailException(request.Email);
        }

        var company = new Company { Name = request.CompanyName };

        var manager = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Role = UserRole.Manager,
            Company = company
        };
        manager.PasswordHash = passwordHasher.HashPassword(manager, request.Password);

        db.Users.Add(manager);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            // A concurrent registration with the same email won the race; the unique index rejected this one.
            if (await EmailExistsAsync(request.Email, cancellationToken))
            {
                throw new DuplicateEmailException(request.Email);
            }

            throw;
        }

        return manager;
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
        // The Email column's case-insensitive collation makes this comparison case-insensitive.
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
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
    /// Returns the user with their company, or null if the account no longer exists.
    /// </summary>
    public Task<User?> GetUserAsync(int userId, CancellationToken cancellationToken = default) =>
        db.Users.Include(u => u.Company).SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

    /// <summary>
    /// Whether the account still exists; tokens of removed users are rejected.
    /// </summary>
    public Task<bool> UserExistsAsync(int userId, CancellationToken cancellationToken = default) =>
        db.Users.AnyAsync(u => u.Id == userId, cancellationToken);

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

    private static void EnsurePasswordIsStrong(string password, string email)
    {
        if (password.Length < MinPasswordLength)
        {
            throw new WeakPasswordException($"Password must be at least {MinPasswordLength} characters long.");
        }

        // Emails are case-insensitive, so a password differing from the email only by case is still the email.
        if (string.Equals(password, email, StringComparison.OrdinalIgnoreCase))
        {
            throw new WeakPasswordException("Password must not be the same as the email.");
        }
    }

    // The Email column's case-insensitive collation makes this comparison case-insensitive.
    private Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(u => u.Email == email, cancellationToken);
}
