namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when a login is attempted on an account that is locked after too many failed attempts.
/// </summary>
public class AccountLockedException(int minutesRemaining)
    : Exception($"The account is locked. Try again in {minutesRemaining} minute(s).")
{
    /// <summary>
    /// Whole minutes until the lock ends, rounded up.
    /// </summary>
    public int MinutesRemaining { get; } = minutesRemaining;
}
