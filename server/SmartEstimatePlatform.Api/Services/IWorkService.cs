using SmartEstimatePlatform.Api.Models.DTOs.Works;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Manages the current company's work catalog. The company scoping from feature 011 means another
/// company's works are never visible here and are reported as not found.
/// </summary>
public interface IWorkService
{
    /// <summary>
    /// Returns the works whose name contains <paramref name="search"/>, ignoring letter case, sorted by name
    /// and unit. An empty search returns every work.
    /// </summary>
    Task<List<WorkResponse>> SearchAsync(string? search);

    /// <summary>
    /// Returns one work of the current company with its composition.
    /// </summary>
    /// <exception cref="NotFoundException">No work with this ID in the current company.</exception>
    Task<WorkResponse> GetAsync(int id);

    /// <summary>
    /// Adds a work with its material lines to the current company's catalog.
    /// The request has already checked the values (WorkRules).
    /// </summary>
    /// <exception cref="UnknownMaterialException">A line uses a material that is not in the company's catalog.</exception>
    /// <exception cref="DuplicateWorkException">The same name and unit already exist.</exception>
    Task<WorkResponse> CreateAsync(WorkRequest request);

    /// <summary>
    /// Changes a work's fields and replaces its whole composition with the request's lines.
    /// </summary>
    /// <exception cref="NotFoundException">No work with this ID in the current company.</exception>
    /// <exception cref="UnknownMaterialException">A line uses a material that is not in the company's catalog.</exception>
    /// <exception cref="DuplicateWorkException">Another work already has the same name and unit.</exception>
    Task<WorkResponse> UpdateAsync(int id, WorkRequest request);

    /// <summary>
    /// Deletes a work and its material lines. Estimates keep their own copies, so they are not affected.
    /// </summary>
    /// <exception cref="NotFoundException">No work with this ID in the current company.</exception>
    Task DeleteAsync(int id);
}
