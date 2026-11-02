using SmartEstimatePlatform.Api.Models.DTOs.Projects;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Manages the current company's projects. The company scoping from feature 011 means another
/// company's projects are never visible here and are reported as not found.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Returns every project of the current company, sorted by name (then by ID, as names may repeat).
    /// </summary>
    Task<List<ProjectResponse>> ListAsync();

    /// <summary>
    /// Returns one project of the current company.
    /// </summary>
    /// <exception cref="NotFoundException">No project with this ID in the current company.</exception>
    Task<ProjectResponse> GetAsync(int id);

    /// <summary>
    /// Adds a project to the current company. The request has already checked the fields (ProjectRules).
    /// </summary>
    Task<ProjectResponse> CreateAsync(ProjectRequest request);

    /// <summary>
    /// Changes a project's name and description.
    /// </summary>
    /// <exception cref="NotFoundException">No project with this ID in the current company.</exception>
    Task<ProjectResponse> UpdateAsync(int id, ProjectRequest request);

    /// <summary>
    /// Deletes a project permanently.
    /// </summary>
    /// <exception cref="NotFoundException">No project with this ID in the current company.</exception>
    Task DeleteAsync(int id);
}
