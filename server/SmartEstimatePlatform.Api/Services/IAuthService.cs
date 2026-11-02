using SmartEstimatePlatform.Api.Models.DTOs.Auth;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Handles account registration, login with lockout after repeated failures, and looking up the logged-in user.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a visitor: creates their company and makes them its Manager.
    /// The company and the account are saved together, so either both exist or neither does.
    /// </summary>
    /// <returns>The new Manager's token and identity; they are logged in.</returns>
    /// <exception cref="DuplicateEmailException">
    /// The email is already in use, compared case-insensitively. Nothing is created.
    /// </exception>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Saves a new account with a hashed password, after checking that the email is not used by any account
    /// in any company. Used for registration and for new Viewers; the request has already checked the password.
    /// </summary>
    /// <exception cref="DuplicateEmailException">The email is already in use, compared case-insensitively. Nothing is saved.</exception>
    Task CreateAccountAsync(User account, string password);

    /// <summary>
    /// Logs a user in and issues their JWT. A successful login clears any failed-attempt streak.
    /// </summary>
    /// <exception cref="InvalidCredentialsException">
    /// The email is unknown or the password is wrong; the same error is used for both.
    /// A wrong password counts towards the lockout.
    /// </exception>
    /// <exception cref="AccountLockedException">
    /// The account is locked, or this failure was the one that locked it. Raised even when the password is correct.
    /// </exception>
    Task<AuthResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Returns the logged-in user's name, company and role.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">The logged-in user no longer exists in the current company.</exception>
    Task<CurrentUserResponse> GetCurrentUserAsync();

    /// <summary>
    /// Whether a token is still valid for its user: the user still exists, still belongs to the token's
    /// company, and their password has not been reset since the token was issued.
    /// </summary>
    Task<bool> IsTokenUserValidAsync(int userId, int companyId, int tokenVersion);
}
