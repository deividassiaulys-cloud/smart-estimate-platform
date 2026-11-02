using Microsoft.AspNetCore.Mvc;

namespace SmartEstimatePlatform.Api.Controllers;

/// <summary>
/// Base of every API controller. Declares once what all endpoints share: automatic request validation,
/// which rejects an invalid request with 400 before any service runs (Constitution X.8, X.10).
/// </summary>
[ApiController]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
public abstract class ApiControllerBase : ControllerBase;
