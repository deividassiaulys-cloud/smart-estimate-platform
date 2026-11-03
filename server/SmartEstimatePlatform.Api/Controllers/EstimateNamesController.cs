using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Controllers;

/// <summary>
/// The names-only view of estimates for every logged-in user, Viewers included (FR-026). Its responses have no
/// number or status fields, so nothing commercial can reach a Viewer (BR-005). Another company's project or
/// estimate is reported as not found (404).
/// </summary>
[Route("api")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public class EstimateNamesController(IEstimateService estimateService) : ApiControllerBase
{
    /// <summary>
    /// Lists every estimate of a project by name.
    /// </summary>
    [HttpGet("projects/{projectId:int}/estimate-names")]
    [ProducesResponseType<List<EstimateNameResponse>>(StatusCodes.Status200OK)]
    public Task<List<EstimateNameResponse>> List(int projectId) =>
        estimateService.ListNamesForProjectAsync(projectId);

    /// <summary>
    /// Returns an estimate's work and material names.
    /// </summary>
    [HttpGet("estimates/{id:int}/names")]
    [ProducesResponseType<EstimateNamesResponse>(StatusCodes.Status200OK)]
    public Task<EstimateNamesResponse> Get(int id) =>
        estimateService.GetNamesAsync(id);
}
