namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when a registration uses an email that already belongs to an account.
/// </summary>
public class DuplicateEmailException(string email)
    : Exception($"An account with email '{email}' already exists.")
{
    public string Email { get; } = email;
}
