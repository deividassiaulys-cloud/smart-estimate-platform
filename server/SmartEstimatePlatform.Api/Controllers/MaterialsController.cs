using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstimatePlatform.Api.Models.DTOs.Materials;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Controllers;

/// <summary>
/// The Manager's material catalog. Viewers never see it, because it contains prices.
/// Another company's material is reported as not found (404) by MaterialService.
/// </summary>
[Route("api/materials")]
[Authorize(Roles = nameof(UserRole.Manager))]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class MaterialsController(IMaterialService materialService) : ApiControllerBase
{
    /// <summary>
    /// Lists the company's materials sorted by name, optionally only those whose name contains <paramref name="search"/>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<List<MaterialResponse>>(StatusCodes.Status200OK)]
    public Task<List<MaterialResponse>> List([FromQuery] string? search) =>
        materialService.SearchAsync(search);

    /// <summary>
    /// Returns one material, for the edit form.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<MaterialResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<MaterialResponse> Get(int id) =>
        materialService.GetAsync(id);

    /// <summary>
    /// Adds a material to the company's catalog.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<MaterialResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MaterialResponse>> Create(MaterialRequest request) =>
        StatusCode(StatusCodes.Status201Created, await materialService.CreateAsync(request));

    /// <summary>
    /// Changes a material's name, unit and price.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<MaterialResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<MaterialResponse> Update(int id, MaterialRequest request) =>
        materialService.UpdateAsync(id, request);

    /// <summary>
    /// Deletes a material.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await materialService.DeleteAsync(id);
        return NoContent();
    }
}
