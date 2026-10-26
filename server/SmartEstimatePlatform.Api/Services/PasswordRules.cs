namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// The password rules for every account: at registration, when a Manager creates a Viewer,
/// and when a Manager resets a Viewer's password.
/// </summary>
public static class PasswordRules
{
    /// <summary>
    /// Shortest password accepted.
    /// </summary>
    public const int MinLength = 8;

    /// <summary>
    /// Rejects a password that is too short or equals the account's email.
    /// </summary>
    /// <exception cref="WeakPasswordException">The password breaks a rule.</exception>
    public static void EnsureStrong(string password, string email)
    {
        if (password.Length < MinLength)
        {
            throw new WeakPasswordException($"Password must be at least {MinLength} characters long.");
        }

        // Emails are case-insensitive, so a password differing from the email only by case is still the email.
        if (string.Equals(password, email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new WeakPasswordException("Password must not be the same as the email.");
        }
    }
}
