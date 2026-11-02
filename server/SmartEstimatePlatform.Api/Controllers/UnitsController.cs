using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Controllers;

/// <summary>
/// The fixed measurement units, so the frontend never keeps its own copy of the list.
/// Units contain no prices, so any logged-in user may read them.
/// </summary>
[Route("api/units")]
[Authorize]
public class UnitsController : ApiControllerBase
{
    /// <summary>
    /// Lists the allowed measurement units.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public IReadOnlyList<string> List() => MeasurementUnit.All;
}
