using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstimatePlatform.Api.Models.DTOs;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService, TokenService tokenService) : ControllerBase
{
    /// <summary>
    /// Registers a visitor, creating their company and their Manager account, and logs them in.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var manager = await authService.RegisterAsync(request, cancellationToken);

            var response = new RegisterResponse
            {
                Token = tokenService.CreateToken(manager),
                UserId = manager.Id,
                CompanyId = manager.CompanyId,
                Role = manager.Role
            };
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (WeakPasswordException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (DuplicateEmailException)
        {
            return Problem("An account with this email already exists.", statusCode: StatusCodes.Status409Conflict);
        }
    }

    /// <summary>
    /// Logs a user in. Wrong email and wrong password get the same error; a locked account reports the minutes remaining.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await authService.LoginAsync(request, cancellationToken);
        }
        catch (InvalidCredentialsException)
        {
            return Problem("Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);
        }
        catch (AccountLockedException ex)
        {
            var problem = ProblemDetailsFactory.CreateProblemDetails(
                HttpContext, StatusCodes.Status423Locked, detail: ex.Message);
            problem.Extensions["minutesRemaining"] = ex.MinutesRemaining;
            return new ObjectResult(problem) { StatusCode = StatusCodes.Status423Locked };
        }
    }

    /// <summary>
    /// Returns the logged-in user's name, company and role.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        var user = await authService.GetUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return new CurrentUserResponse
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            CompanyName = user.Company.Name,
            Role = user.Role
        };
    }
}
