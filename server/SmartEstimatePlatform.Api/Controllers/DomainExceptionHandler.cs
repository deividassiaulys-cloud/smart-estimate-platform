using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Controllers;

/// <summary>
/// The one place where errors raised by the services become HTTP error responses, so controllers need no
/// error handling (Constitution X.2, X.8). Other exceptions are left to the default handling.
/// The responses are built by MVC's problem details factory, exactly as controllers build them.
/// </summary>
public class DomainExceptionHandler(ProblemDetailsFactory problemDetailsFactory) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (ToProblem(httpContext, exception) is not { } problem)
        {
            return false;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, options: null, "application/problem+json", cancellationToken);
        return true;
    }

    // Duplicate errors use fixed texts, because their exception messages name the email, material or work.
    private ProblemDetails? ToProblem(HttpContext httpContext, Exception exception)
    {
        ProblemDetails Problem(int status, string? detail, string? title = null) =>
            problemDetailsFactory.CreateProblemDetails(httpContext, status, title, detail: detail);

        switch (exception)
        {
            case NotFoundException:
                return Problem(StatusCodes.Status404NotFound, exception.Message, title: "Not found");
            case WeakPasswordException:
                return Problem(StatusCodes.Status400BadRequest, exception.Message);
            case DuplicateEmailException:
                return Problem(StatusCodes.Status409Conflict, "An account with this email already exists.");
            case DuplicateMaterialException:
                return Problem(StatusCodes.Status409Conflict, "A material with this name and unit already exists.");
            case DuplicateWorkException:
                return Problem(StatusCodes.Status409Conflict, "A work with this name and unit already exists.");
            case UnknownMaterialException:
                return Problem(StatusCodes.Status400BadRequest, "Each line must use a material from your catalog.");
            case MaterialUnitMismatchException:
                return Problem(StatusCodes.Status400BadRequest, exception.Message);
            case UnknownWorkException:
                return Problem(StatusCodes.Status400BadRequest, "The work is not in your catalog.");
            case EstimateNotDraftException or EstimateStatusException:
                return Problem(StatusCodes.Status409Conflict, exception.Message);
            case MaterialInUseException inUse:
                var inUseProblem = Problem(StatusCodes.Status409Conflict, inUse.Message);
                inUseProblem.Extensions["works"] = inUse.WorkNames;
                return inUseProblem;
            case InvalidCredentialsException:
                return Problem(StatusCodes.Status401Unauthorized, exception.Message);
            case AccountLockedException locked:
                var problem = Problem(StatusCodes.Status423Locked, locked.Message);
                problem.Extensions["minutesRemaining"] = locked.MinutesRemaining;
                return problem;
            case UnauthorizedAccessException:
                return Problem(StatusCodes.Status401Unauthorized, detail: null);
            default:
                return null;
        }
    }
}
