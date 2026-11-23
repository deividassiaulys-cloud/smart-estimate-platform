using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstimatePlatform.Api.Models.DTOs.CatalogSections;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Controllers;

/// <summary>
/// The Manager's catalog sections (FR-030). Viewers never see them, because they belong to the priced catalog.
/// Another company's section is reported as not found (404) by CatalogSectionService.
/// </summary>
[Route("api/catalog-sections")]
[Authorize(Roles = nameof(UserRole.Manager))]
public class CatalogSectionsController(ICatalogSectionService catalogSectionService) : ApiControllerBase
{
    /// <summary>
    /// Lists the company's sections sorted by name, optionally only those whose name contains <paramref name="search"/>.
    /// </summary>
    [HttpGet]
    public Task<List<CatalogSectionResponse>> List([FromQuery, StringLength(SearchPattern.MaxLength)] string? search) =>
        catalogSectionService.SearchAsync(search);

    /// <summary>
    /// Returns one section with its works, for the edit form.
    /// </summary>
    [HttpGet("{id:int}")]
    public Task<CatalogSectionResponse> Get(int id) =>
        catalogSectionService.GetAsync(id);

    /// <summary>
    /// Adds a section to the company's catalog.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CatalogSectionResponse>> Create(CatalogSectionRequest request) =>
        StatusCode(StatusCodes.Status201Created, await catalogSectionService.CreateAsync(request));

    /// <summary>
    /// Changes a section's name and unit and replaces its works.
    /// </summary>
    [HttpPut("{id:int}")]
    public Task<CatalogSectionResponse> Update(int id, CatalogSectionRequest request) =>
        catalogSectionService.UpdateAsync(id, request);

    /// <summary>
    /// Deletes a section.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await catalogSectionService.DeleteAsync(id);
        return NoContent();
    }
}
