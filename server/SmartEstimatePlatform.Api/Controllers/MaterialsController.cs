using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstimatePlatform.Api.Models.DTOs;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Controllers;

/// <summary>
/// The Manager's material catalog. Viewers never see it, because it contains prices.
/// Another company's material is reported as not found (404) by MaterialService.
/// </summary>
[ApiController]
[Route("api/materials")]
[Authorize(Roles = nameof(UserRole.Manager))]
public class MaterialsController(MaterialService materialService) : ControllerBase
{
    /// <summary>
    /// Lists the company's materials sorted by name, optionally only those whose name contains <paramref name="search"/>.
    /// </summary>
    [HttpGet]
    public async Task<IEnumerable<MaterialResponse>> List([FromQuery] string? search, CancellationToken cancellationToken) =>
        (await materialService.SearchAsync(search, cancellationToken)).Select(MaterialResponse.From);

    /// <summary>
    /// Returns one material, for the edit form.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<MaterialResponse> Get(int id, CancellationToken cancellationToken) =>
        MaterialResponse.From(await materialService.GetAsync(id, cancellationToken));

    /// <summary>
    /// Adds a material to the company's catalog.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<MaterialResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<MaterialResponse>> Create(MaterialRequest request, CancellationToken cancellationToken) =>
        Save(async () => StatusCode(StatusCodes.Status201Created,
            MaterialResponse.From(await materialService.CreateAsync(request, cancellationToken))));

    /// <summary>
    /// Changes a material's name, unit and price.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<MaterialResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<MaterialResponse>> Update(int id, MaterialRequest request, CancellationToken cancellationToken) =>
        Save(async () => Ok(MaterialResponse.From(await materialService.UpdateAsync(id, request, cancellationToken))));

    /// <summary>
    /// Deletes a material.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await materialService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private async Task<ActionResult<MaterialResponse>> Save(Func<Task<ActionResult>> save)
    {
        try
        {
            return await save();
        }
        catch (InvalidMaterialException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (DuplicateMaterialException)
        {
            return Problem("A material with this name and unit already exists.", statusCode: StatusCodes.Status409Conflict);
        }
    }
}
