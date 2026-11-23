using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartEstimatePlatform.Api.Models.DTOs.Auth;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Controllers;

[Route("api/auth")]
public class AuthController(IAuthService authService) : ApiControllerBase
{
    /// <summary>
    /// Registers a visitor, creating their company and their Manager account, and logs them in.
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting(AuthRateLimiting.PolicyName)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request) =>
        StatusCode(StatusCodes.Status201Created, await authService.RegisterAsync(request));

    /// <summary>
    /// Logs a user in. Wrong email and wrong password get the same error; a locked account reports the minutes remaining.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting(AuthRateLimiting.PolicyName)]
    public Task<AuthResponse> Login(LoginRequest request) =>
        authService.LoginAsync(request);

    /// <summary>
    /// Returns the logged-in user's name, company and role.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public Task<CurrentUserResponse> Me() =>
        authService.GetCurrentUserAsync();

    /// <summary>
    /// Logs the user out: every token issued to them so far stops working.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await authService.LogoutAsync();
        return NoContent();
    }
}
