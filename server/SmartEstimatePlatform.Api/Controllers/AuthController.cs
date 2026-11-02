using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request) =>
        StatusCode(StatusCodes.Status201Created, await authService.RegisterAsync(request));

    /// <summary>
    /// Logs a user in. Wrong email and wrong password get the same error; a locked account reports the minutes remaining.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public Task<AuthResponse> Login(LoginRequest request) =>
        authService.LoginAsync(request);

    /// <summary>
    /// Returns the logged-in user's name, company and role.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<CurrentUserResponse> Me() =>
        authService.GetCurrentUserAsync();
}
