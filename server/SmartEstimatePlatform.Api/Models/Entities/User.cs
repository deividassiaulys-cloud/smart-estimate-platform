namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// An account that belongs to exactly one company and acts in that company with a single role.
/// </summary>
public class User : ICompanyScoped
{
    // Used by EF Core when loading a user.
    private User()
    {
    }

    /// <summary>
    /// Creates an account in an existing company. The password hash is set separately.
    /// </summary>
    public User(string firstName, string lastName, string email, UserRole role, int companyId)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Role = role;
        CompanyId = companyId;
    }

    /// <summary>
    /// Creates an account together with its new company (registration). The password hash is set separately.
    /// </summary>
    public User(string firstName, string lastName, string email, UserRole role, Company company)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Role = role;
        Company = company;
    }

    public int Id { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    /// <summary>
    /// Login email. Unique across all accounts, compared case-insensitively.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Hash of the user's password. The plain-text password is never stored.
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public int CompanyId { get; private set; }

    public Company Company { get; private set; } = null!;

    /// <summary>
    /// Failed login attempts in the current streak, counted from <see cref="FirstFailedLoginAt"/>.
    /// </summary>
    public int FailedLoginCount { get; private set; }

    /// <summary>
    /// When the current streak of failed logins started (UTC); null when there is no streak.
    /// </summary>
    public DateTime? FirstFailedLoginAt { get; private set; }

    /// <summary>
    /// Until when the account is locked (UTC); null or in the past when it is not locked.
    /// </summary>
    public DateTime? LockoutEnd { get; private set; }

    /// <summary>
    /// Increases when the password is reset; tokens carrying an older version are rejected.
    /// </summary>
    public int TokenVersion { get; private set; }

    /// <summary>
    /// Stores a new password hash.
    /// </summary>
    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;

    /// <summary>
    /// Stores the failed-login streak and lock as calculated by the login rules.
    /// </summary>
    public void SetLoginState(int failedLoginCount, DateTime? firstFailedLoginAt, DateTime? lockoutEnd)
    {
        FailedLoginCount = failedLoginCount;
        FirstFailedLoginAt = firstFailedLoginAt;
        LockoutEnd = lockoutEnd;
    }

    /// <summary>
    /// Clears any failed-login streak and lock.
    /// </summary>
    public void ResetLoginFailures() => SetLoginState(0, null, null);

    /// <summary>
    /// Makes every token issued so far invalid.
    /// </summary>
    public void InvalidateTokens() => TokenVersion++;
}
