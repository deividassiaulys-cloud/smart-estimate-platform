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
}
