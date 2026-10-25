namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when a registration password does not meet the password rules:
/// at least 8 characters and not equal to the email.
/// </summary>
public class WeakPasswordException(string message) : Exception(message);
