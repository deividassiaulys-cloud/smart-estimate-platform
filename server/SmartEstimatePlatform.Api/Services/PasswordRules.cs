namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// The password rules for every account: at registration, when a Manager creates a Viewer,
/// and when a Manager resets a Viewer's password. The requests apply them declaratively (Constitution X.10).
/// </summary>
public static class PasswordRules
{
    /// <summary>
    /// Shortest password accepted.
    /// </summary>
    public const int MinLength = 8;

    private const string SameAsEmailMessage = "Password must not be the same as the email.";

    /// <summary>
    /// Returns why the password is too weak, or null when it is strong enough. The email check runs only
    /// when <paramref name="email"/> is given.
    /// </summary>
    public static string? Validate(string password, string? email)
    {
        if (password.Length < MinLength)
        {
            return $"Password must be at least {MinLength} characters long.";
        }

        return email is not null && IsSameAsEmail(password, email) ? SameAsEmailMessage : null;
    }

    /// <summary>
    /// Rejects a password that equals an email the request itself does not carry, such as a Viewer's stored
    /// email when their password is reset.
    /// </summary>
    /// <exception cref="WeakPasswordException">The password equals the email.</exception>
    public static void EnsureNotEmail(string password, string email)
    {
        if (IsSameAsEmail(password, email))
        {
            throw new WeakPasswordException(SameAsEmailMessage);
        }
    }

    // Emails are case-insensitive, so a password differing from the email only by case is still the email.
    private static bool IsSameAsEmail(string password, string email) =>
        string.Equals(password, email.Trim(), StringComparison.OrdinalIgnoreCase);
}
