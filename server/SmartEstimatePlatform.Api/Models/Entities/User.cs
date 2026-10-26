namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// An account that belongs to exactly one company and acts in that company with a single role.
/// </summary>
public class User : ICompanyScoped
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Login email. Unique across all accounts, compared case-insensitively.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the user's password. The plain-text password is never stored.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public int CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    /// <summary>
    /// Failed login attempts in the current streak, counted from <see cref="FirstFailedLoginAt"/>.
    /// </summary>
    public int FailedLoginCount { get; set; }

    /// <summary>
    /// When the current streak of failed logins started (UTC); null when there is no streak.
    /// </summary>
    public DateTime? FirstFailedLoginAt { get; set; }

    /// <summary>
    /// Until when the account is locked (UTC); null or in the past when it is not locked.
    /// </summary>
    public DateTime? LockoutEnd { get; set; }
}
