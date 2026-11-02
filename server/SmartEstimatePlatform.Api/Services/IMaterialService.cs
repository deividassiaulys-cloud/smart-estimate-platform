using SmartEstimatePlatform.Api.Models.DTOs.Materials;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Manages the current company's material catalog. The company scoping from feature 011 means another
/// company's materials are never visible here and are reported as not found.
/// </summary>
public interface IMaterialService
{
    /// <summary>
    /// Returns the materials whose name contains <paramref name="search"/>, ignoring letter case, sorted by name.
    /// An empty search returns every material.
    /// </summary>
    Task<List<MaterialResponse>> SearchAsync(string? search);

    /// <summary>
    /// Returns one material of the current company.
    /// </summary>
    /// <exception cref="NotFoundException">No material with this ID in the current company.</exception>
    Task<MaterialResponse> GetAsync(int id);

    /// <summary>
    /// Adds a material to the current company's catalog.
    /// </summary>
    /// <exception cref="DuplicateMaterialException">The same name and unit already exist.</exception>
    Task<MaterialResponse> CreateAsync(MaterialRequest request);

    /// <summary>
    /// Changes a material's name, unit and price.
    /// </summary>
    /// <exception cref="NotFoundException">No material with this ID in the current company.</exception>
    /// <exception cref="DuplicateMaterialException">Another material already has the same name and unit.</exception>
    Task<MaterialResponse> UpdateAsync(int id, MaterialRequest request);

    /// <summary>
    /// Deletes a material. Estimates keep their own copies of material data, so they are not affected.
    /// </summary>
    /// <exception cref="MaterialInUseException">A work's composition uses the material; it is not deleted.</exception>
    /// <exception cref="NotFoundException">No material with this ID in the current company.</exception>
    Task DeleteAsync(int id);
}
