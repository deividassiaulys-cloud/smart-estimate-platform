namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// An account that belongs to exactly one company and acts in that company with a single role.
/// </summary>
public class User
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
}
