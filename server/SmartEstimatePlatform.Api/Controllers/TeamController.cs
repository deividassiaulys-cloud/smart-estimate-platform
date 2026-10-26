using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstimatePlatform.Api.Models.DTOs;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Controllers;

/// <summary>
/// The Manager's Team page: their company's members and the Viewer accounts they manage.
/// A Viewer of another company is reported as not found (404) by TeamService.
/// </summary>
[ApiController]
[Route("api/team")]
[Authorize(Roles = nameof(UserRole.Manager))]
public class TeamController(TeamService teamService) : ControllerBase
{
    /// <summary>
    /// Lists everyone in the Manager's company.
    /// </summary>
    [HttpGet]
    public async Task<IEnumerable<TeamMemberResponse>> List(CancellationToken cancellationToken) =>
        (await teamService.ListMembersAsync(cancellationToken)).Select(TeamMemberResponse.From);

    /// <summary>
    /// Creates a Viewer account in the Manager's company.
    /// </summary>
    [HttpPost("viewers")]
    [ProducesResponseType<TeamMemberResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TeamMemberResponse>> CreateViewer(
        CreateViewerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var viewer = await teamService.CreateViewerAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, TeamMemberResponse.From(viewer));
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
    /// Removes a Viewer; their access ends immediately.
    /// </summary>
    [HttpDelete("viewers/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveViewer(int id, CancellationToken cancellationToken)
    {
        await teamService.RemoveViewerAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Sets a new password for a Viewer and logs out their open sessions.
    /// </summary>
    [HttpPatch("viewers/{id:int}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await teamService.ResetPasswordAsync(id, request.NewPassword, cancellationToken);
            return NoContent();
        }
        catch (WeakPasswordException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
