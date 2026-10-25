using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Handles account registration for visitors.
/// </summary>
public class AuthService(AppDbContext db, IPasswordHasher<User> passwordHasher)
{
    /// <summary>
    /// Shortest password accepted at registration.
    /// </summary>
    public const int MinPasswordLength = 8;

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
