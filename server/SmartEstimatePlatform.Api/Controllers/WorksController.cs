using System.ComponentModel.DataAnnotations;
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
public class WorksController(IWorkService workService) : ApiControllerBase
{
    /// <summary>
    /// Lists the company's works sorted by name, optionally only those whose name contains <paramref name="search"/>.
    /// </summary>
    [HttpGet]
    public Task<List<WorkResponse>> List([FromQuery, StringLength(SearchPattern.MaxLength)] string? search) =>
        workService.SearchAsync(search);

    /// <summary>
    /// Returns one work with its composition, for the edit form.
    /// </summary>
    [HttpGet("{id:int}")]
    public Task<WorkResponse> Get(int id) =>
        workService.GetAsync(id);

    /// <summary>
    /// Adds a work with its material lines to the company's catalog.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WorkResponse>> Create(WorkRequest request) =>
        StatusCode(StatusCodes.Status201Created, await workService.CreateAsync(request));

    /// <summary>
    /// Changes a work and replaces its whole composition.
    /// </summary>
    [HttpPut("{id:int}")]
    public Task<WorkResponse> Update(int id, WorkRequest request) =>
        workService.UpdateAsync(id, request);

    /// <summary>
    /// Deletes a work.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await workService.DeleteAsync(id);
        return NoContent();
    }
}
