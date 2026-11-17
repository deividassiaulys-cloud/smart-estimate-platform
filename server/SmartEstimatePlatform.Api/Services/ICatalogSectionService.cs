using SmartEstimatePlatform.Api.Models.DTOs.CatalogSections;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Manages the current company's catalog sections (FR-030). The company scoping from feature 011 means another
/// company's sections and works are never visible here and are reported as not found.
/// </summary>
public interface ICatalogSectionService
{
    /// <summary>
    /// Returns the sections whose name contains <paramref name="search"/>, ignoring letter case, sorted by name and unit.
    /// An empty search returns every section.
    /// </summary>
    Task<List<CatalogSectionResponse>> SearchAsync(string? search);

    /// <summary>
    /// Returns one section of the current company with its works.
    /// </summary>
    /// <exception cref="NotFoundException">No section with this ID in the current company.</exception>
    Task<CatalogSectionResponse> GetAsync(int id);

    /// <summary>
    /// Adds a section with its works to the current company's catalog. The request has already checked the values.
    /// </summary>
    /// <exception cref="UnknownWorkException">A line uses a work that is not in the company's catalog.</exception>
    /// <exception cref="DuplicateCatalogSectionException">The same name and unit already exist.</exception>
    Task<CatalogSectionResponse> CreateAsync(CatalogSectionRequest request);

    /// <summary>
    /// Changes a section's name and unit and replaces its whole list of works. Estimates are not affected (BR-010).
    /// </summary>
    /// <exception cref="NotFoundException">No section with this ID in the current company.</exception>
    /// <exception cref="UnknownWorkException">A line uses a work that is not in the company's catalog.</exception>
    /// <exception cref="DuplicateCatalogSectionException">Another section already has the same name and unit.</exception>
    Task<CatalogSectionResponse> UpdateAsync(int id, CatalogSectionRequest request);

    /// <summary>
    /// Deletes a section and its lines. Estimates keep their own sections and lines, so they are not affected.
    /// </summary>
    /// <exception cref="NotFoundException">No section with this ID in the current company.</exception>
    Task DeleteAsync(int id);
}
