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
public class EstimatesController(IEstimateService estimateService, IEstimateExportService exportService) : ApiControllerBase
{
    /// <summary>
    /// Lists a project's estimates by name, with status and total.
    /// </summary>
    [HttpGet("projects/{projectId:int}/estimates")]
    public Task<List<EstimateSummaryResponse>> List(int projectId) =>
        estimateService.ListForProjectAsync(projectId);

    /// <summary>
    /// Creates a Draft estimate in a project.
    /// </summary>
    [HttpPost("projects/{projectId:int}/estimates")]
    public async Task<ActionResult<EstimateResponse>> Create(int projectId, CreateEstimateRequest request) =>
        StatusCode(StatusCodes.Status201Created, await estimateService.CreateAsync(projectId, request));

    /// <summary>
    /// Returns one estimate with every amount calculated.
    /// </summary>
    [HttpGet("estimates/{id:int}")]
    public Task<EstimateResponse> Get(int id) =>
        estimateService.GetAsync(id);

    /// <summary>
    /// Adds a catalog work with a quantity and generates its material lines.
    /// </summary>
    [HttpPost("estimates/{id:int}/lines")]
    public Task<EstimateResponse> AddWorkLine(int id, AddWorkLineRequest request) =>
        estimateService.AddWorkLineAsync(id, request);

    /// <summary>
    /// Changes the quantity of a work line.
    /// </summary>
    [HttpPatch("estimates/{id:int}/lines/{lineId:int}")]
    public Task<EstimateResponse> ChangeQuantity(int id, int lineId, ChangeQuantityRequest request) =>
        estimateService.ChangeQuantityAsync(id, lineId, request);

    /// <summary>
    /// Removes a work line together with its material lines.
    /// </summary>
    [HttpDelete("estimates/{id:int}/lines/{lineId:int}")]
    public Task<EstimateResponse> RemoveWorkLine(int id, int lineId) =>
        estimateService.RemoveWorkLineAsync(id, lineId);

    /// <summary>
    /// Adds a catalog section with a section quantity to a Draft estimate (FR-031).
    /// </summary>
    [HttpPost("estimates/{id:int}/sections")]
    public Task<EstimateResponse> AddSection(int id, AddSectionRequest request) =>
        estimateService.AddSectionAsync(id, request);

    /// <summary>
    /// Changes a section's quantity, recalculating its works from the catalog section (FR-032).
    /// </summary>
    [HttpPatch("estimates/{id:int}/sections/{sectionId:int}")]
    public Task<EstimateResponse> ChangeSectionQuantity(int id, int sectionId, ChangeSectionQuantityRequest request) =>
        estimateService.ChangeSectionQuantityAsync(id, sectionId, request);

    /// <summary>
    /// Removes a section with its work lines (FR-033).
    /// </summary>
    [HttpDelete("estimates/{id:int}/sections/{sectionId:int}")]
    public Task<EstimateResponse> RemoveSection(int id, int sectionId) =>
        estimateService.RemoveSectionAsync(id, sectionId);

    /// <summary>
    /// Changes a work line's labour rate in this estimate only.
    /// </summary>
    [HttpPatch("estimates/{id:int}/lines/{lineId:int}/labour-rate")]
    public Task<EstimateResponse> ChangeLabourRate(int id, int lineId, ChangeLabourRateRequest request) =>
        estimateService.ChangeLabourRateAsync(id, lineId, request);

    /// <summary>
    /// Changes a material line's price in this estimate only.
    /// </summary>
    [HttpPatch("estimates/{id:int}/lines/{lineId:int}/materials/{materialLineId:int}/price")]
    public Task<EstimateResponse> ChangeMaterialPrice(int id, int lineId, int materialLineId, ChangePriceRequest request) =>
        estimateService.ChangeMaterialPriceAsync(id, lineId, materialLineId, request);

    /// <summary>
    /// Replaces a material line's material with a same-unit catalog material.
    /// </summary>
    [HttpPatch("estimates/{id:int}/lines/{lineId:int}/materials/{materialLineId:int}/material")]
    public Task<EstimateResponse> ReplaceMaterial(int id, int lineId, int materialLineId, ReplaceMaterialRequest request) =>
        estimateService.ReplaceMaterialAsync(id, lineId, materialLineId, request);

    /// <summary>
    /// Removes one material line from a work line.
    /// </summary>
    [HttpDelete("estimates/{id:int}/lines/{lineId:int}/materials/{materialLineId:int}")]
    public Task<EstimateResponse> RemoveMaterialLine(int id, int lineId, int materialLineId) =>
        estimateService.RemoveMaterialLineAsync(id, lineId, materialLineId);

    /// <summary>
    /// Shows what updating the estimate from the current catalog would change; nothing is saved.
    /// </summary>
    [HttpGet("estimates/{id:int}/refresh")]
    public Task<EstimateRefreshPreviewResponse> PreviewRefresh(int id) =>
        estimateService.PreviewRefreshAsync(id);

    /// <summary>
    /// Updates the estimate's prices and norms from the current catalog.
    /// </summary>
    [HttpPost("estimates/{id:int}/refresh")]
    public Task<EstimateResponse> Refresh(int id) =>
        estimateService.RefreshAsync(id);

    /// <summary>
    /// Confirms a Draft estimate; it can then no longer be changed.
    /// </summary>
    [HttpPatch("estimates/{id:int}/confirm")]
    public Task<EstimateResponse> Confirm(int id) =>
        estimateService.ConfirmAsync(id);

    /// <summary>
    /// Returns a Confirmed estimate to Draft.
    /// </summary>
    [HttpPatch("estimates/{id:int}/draft")]
    public Task<EstimateResponse> ReturnToDraft(int id) =>
        estimateService.ReturnToDraftAsync(id);

    /// <summary>
    /// Downloads the estimate as an Excel file with the same data the editor shows, Draft or Confirmed.
    /// </summary>
    [HttpGet("estimates/{id:int}/export")]
    public async Task<FileContentResult> Export(int id)
    {
        var file = await exportService.ExportAsync(id);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>
    /// Changes the overhead, profit and VAT percentages.
    /// </summary>
    [HttpPatch("estimates/{id:int}/percentages")]
    public Task<EstimateResponse> ChangePercentages(int id, ChangePercentagesRequest request) =>
        estimateService.ChangePercentagesAsync(id, request);
}
