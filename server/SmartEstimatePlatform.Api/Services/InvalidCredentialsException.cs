namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when a login fails because the email or the password is wrong.
/// Deliberately does not say which one.
/// </summary>
public class InvalidCredentialsException() : Exception("Invalid email or password.");
