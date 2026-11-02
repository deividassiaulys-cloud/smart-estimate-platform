using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstimatePlatform.Api.Models.DTOs.Team;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Controllers;

/// <summary>
/// The Manager's Team page: their company's members and the Viewer accounts they manage.
/// A Viewer of another company is reported as not found (404) by TeamService.
/// </summary>
[Route("api/team")]
[Authorize(Roles = nameof(UserRole.Manager))]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class TeamController(ITeamService teamService) : ApiControllerBase
{
    /// <summary>
    /// Lists everyone in the Manager's company.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<List<TeamMemberResponse>>(StatusCodes.Status200OK)]
    public Task<List<TeamMemberResponse>> List() =>
        teamService.ListMembersAsync();

    /// <summary>
    /// Creates a Viewer account in the Manager's company.
    /// </summary>
    [HttpPost("viewers")]
    [ProducesResponseType<TeamMemberResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TeamMemberResponse>> CreateViewer(CreateViewerRequest request) =>
        StatusCode(StatusCodes.Status201Created, await teamService.CreateViewerAsync(request));

    /// <summary>
    /// Removes a Viewer; their access ends immediately.
    /// </summary>
    [HttpDelete("viewers/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveViewer(int id)
    {
        await teamService.RemoveViewerAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Sets a new password for a Viewer and logs out their open sessions.
    /// </summary>
    [HttpPatch("viewers/{id:int}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request)
    {
        await teamService.ResetPasswordAsync(id, request.NewPassword);
        return NoContent();
    }
}
