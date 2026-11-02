using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstimatePlatform.Api.Models.DTOs.Projects;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Controllers;

/// <summary>
/// The company's projects. Every logged-in user may read them (Viewers navigate through projects);
/// only a Manager may create, change or delete one. Another company's project is reported as not found (404).
/// </summary>
[Route("api/projects")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class ProjectsController(IProjectService projectService) : ApiControllerBase
{
    /// <summary>
    /// Lists the company's projects sorted by name.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<List<ProjectResponse>>(StatusCodes.Status200OK)]
    public Task<List<ProjectResponse>> List() =>
        projectService.ListAsync();

    /// <summary>
    /// Returns one project, for Project Detail and the edit form.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ProjectResponse> Get(int id) =>
        projectService.GetAsync(id);

    /// <summary>
    /// Creates a project.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Manager))]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProjectResponse>> Create(ProjectRequest request) =>
        StatusCode(StatusCodes.Status201Created, await projectService.CreateAsync(request));

    /// <summary>
    /// Changes a project's name and description.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Manager))]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ProjectResponse> Update(int id, ProjectRequest request) =>
        projectService.UpdateAsync(id, request);

    /// <summary>
    /// Deletes a project permanently.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Manager))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await projectService.DeleteAsync(id);
        return NoContent();
    }
}
