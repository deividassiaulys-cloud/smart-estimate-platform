using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Controllers;

/// <summary>
/// Estimates and the Estimate Editor. Manager only: estimates contain prices, and the Viewer's names-only view
/// comes with feature 009. Every editor action answers with the whole recalculated estimate.
/// </summary>
[Route("api")]
[Authorize(Roles = nameof(UserRole.Manager))]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public class EstimatesController(IEstimateService estimateService, IEstimateExportService exportService) : ApiControllerBase
{
    /// <summary>
    /// Lists a project's estimates by name, with status and total.
    /// </summary>
    [HttpGet("projects/{projectId:int}/estimates")]
    [ProducesResponseType<List<EstimateSummaryResponse>>(StatusCodes.Status200OK)]
    public Task<List<EstimateSummaryResponse>> List(int projectId) =>
        estimateService.ListForProjectAsync(projectId);

    /// <summary>
    /// Creates a Draft estimate in a project.
    /// </summary>
    [HttpPost("projects/{projectId:int}/estimates")]
    [ProducesResponseType<EstimateResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<EstimateResponse>> Create(int projectId, CreateEstimateRequest request) =>
        StatusCode(StatusCodes.Status201Created, await estimateService.CreateAsync(projectId, request));

    /// <summary>
    /// Returns one estimate with every amount calculated.
    /// </summary>
    [HttpGet("estimates/{id:int}")]
    [ProducesResponseType<EstimateResponse>(StatusCodes.Status200OK)]
    public Task<EstimateResponse> Get(int id) =>
        estimateService.GetAsync(id);

    /// <summary>
    /// Adds a catalog work with a quantity and generates its material lines.
    /// </summary>
    [HttpPost("estimates/{id:int}/lines")]
    [ProducesResponseType<EstimateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<EstimateResponse> AddWorkLine(int id, AddWorkLineRequest request) =>
        estimateService.AddWorkLineAsync(id, request);

    /// <summary>
    /// Changes the quantity of a work line.
    /// </summary>
    [HttpPatch("estimates/{id:int}/lines/{lineId:int}")]
    [ProducesResponseType<EstimateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<EstimateResponse> ChangeQuantity(int id, int lineId, ChangeQuantityRequest request) =>
        estimateService.ChangeQuantityAsync(id, lineId, request);

    /// <summary>
    /// Removes a work line together with its material lines.
    /// </summary>
    [HttpDelete("estimates/{id:int}/lines/{lineId:int}")]
    [ProducesResponseType<EstimateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<EstimateResponse> RemoveWorkLine(int id, int lineId) =>
        estimateService.RemoveWorkLineAsync(id, lineId);

    /// <summary>
    /// Confirms a Draft estimate; it can then no longer be changed.
    /// </summary>
    [HttpPatch("estimates/{id:int}/confirm")]
    [ProducesResponseType<EstimateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<EstimateResponse> Confirm(int id) =>
        estimateService.ConfirmAsync(id);

    /// <summary>
    /// Returns a Confirmed estimate to Draft.
    /// </summary>
    [HttpPatch("estimates/{id:int}/draft")]
    [ProducesResponseType<EstimateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<EstimateResponse> ReturnToDraft(int id) =>
        estimateService.ReturnToDraftAsync(id);

    /// <summary>
    /// Downloads the estimate as an Excel file with the same data the editor shows, Draft or Confirmed.
    /// </summary>
    [HttpGet("estimates/{id:int}/export")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<FileContentResult> Export(int id)
    {
        var file = await exportService.ExportAsync(id);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>
    /// Changes the overhead, profit and VAT percentages.
    /// </summary>
    [HttpPatch("estimates/{id:int}/percentages")]
    [ProducesResponseType<EstimateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<EstimateResponse> ChangePercentages(int id, ChangePercentagesRequest request) =>
        estimateService.ChangePercentagesAsync(id, request);
}
