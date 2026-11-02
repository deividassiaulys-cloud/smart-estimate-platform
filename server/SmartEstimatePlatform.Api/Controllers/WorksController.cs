using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstimatePlatform.Api.Models.DTOs.Works;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Controllers;

/// <summary>
/// The Manager's work catalog. Viewers never see it, because works contain labour prices.
/// Another company's work is reported as not found (404) by WorkService.
/// </summary>
[Route("api/works")]
[Authorize(Roles = nameof(UserRole.Manager))]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class WorksController(IWorkService workService) : ApiControllerBase
{
    /// <summary>
    /// Lists the company's works sorted by name, optionally only those whose name contains <paramref name="search"/>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<List<WorkResponse>>(StatusCodes.Status200OK)]
    public Task<List<WorkResponse>> List([FromQuery] string? search) =>
        workService.SearchAsync(search);

    /// <summary>
    /// Returns one work with its composition, for the edit form.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<WorkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WorkResponse> Get(int id) =>
        workService.GetAsync(id);

    /// <summary>
    /// Adds a work with its material lines to the company's catalog.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<WorkResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkResponse>> Create(WorkRequest request) =>
        StatusCode(StatusCodes.Status201Created, await workService.CreateAsync(request));

    /// <summary>
    /// Changes a work and replaces its whole composition.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<WorkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<WorkResponse> Update(int id, WorkRequest request) =>
        workService.UpdateAsync(id, request);

    /// <summary>
    /// Deletes a work.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await workService.DeleteAsync(id);
        return NoContent();
    }
}
