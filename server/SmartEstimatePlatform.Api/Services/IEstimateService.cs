using SmartEstimatePlatform.Api.Models.DTOs.Estimates;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Creates and edits the current company's estimates. Every change is saved at once and answered with the whole
/// estimate, recalculated by <see cref="EstimateCalculator"/>. Only Draft estimates can be changed (BR-013).
/// Another company's project, estimate or line is reported as not found.
/// </summary>
public interface IEstimateService
{
    /// <summary>
    /// Lists a project's estimates by name, each with its total.
    /// </summary>
    /// <exception cref="NotFoundException">No project with this ID in the current company.</exception>
    Task<List<EstimateSummaryResponse>> ListForProjectAsync(int projectId);

    /// <summary>
    /// Creates a Draft estimate in a project with no lines, overhead and profit at 0 % and VAT at 21 %.
    /// </summary>
    /// <exception cref="NotFoundException">No project with this ID in the current company.</exception>
    Task<EstimateResponse> CreateAsync(int projectId, CreateEstimateRequest request);

    /// <summary>
    /// Returns one estimate with every amount calculated.
    /// </summary>
    /// <exception cref="NotFoundException">No estimate with this ID in the current company.</exception>
    Task<EstimateResponse> GetAsync(int id);

    /// <summary>
    /// Adds a catalog work with a quantity, copying the work's labour data and its materials' prices and norms
    /// (BR-010) and generating its material lines.
    /// </summary>
    /// <exception cref="NotFoundException">No estimate with this ID in the current company.</exception>
    /// <exception cref="EstimateNotDraftException">The estimate is not Draft.</exception>
    /// <exception cref="UnknownWorkException">The work is not in the current company's catalog.</exception>
    Task<EstimateResponse> AddWorkLineAsync(int id, AddWorkLineRequest request);

    /// <summary>
    /// Changes the quantity of one work line.
    /// </summary>
    /// <exception cref="NotFoundException">No such estimate, or no such line in it.</exception>
    /// <exception cref="EstimateNotDraftException">The estimate is not Draft.</exception>
    Task<EstimateResponse> ChangeQuantityAsync(int id, int lineId, ChangeQuantityRequest request);

    /// <summary>
    /// Changes the overhead, profit and VAT percentages.
    /// </summary>
    /// <exception cref="NotFoundException">No estimate with this ID in the current company.</exception>
    /// <exception cref="EstimateNotDraftException">The estimate is not Draft.</exception>
    Task<EstimateResponse> ChangePercentagesAsync(int id, ChangePercentagesRequest request);

    /// <summary>
    /// Confirms a Draft estimate, whatever its number of lines; it can no longer be changed (BR-013).
    /// </summary>
    /// <exception cref="NotFoundException">No estimate with this ID in the current company.</exception>
    /// <exception cref="EstimateStatusException">The estimate is already Confirmed.</exception>
    Task<EstimateResponse> ConfirmAsync(int id);

    /// <summary>
    /// Returns a Confirmed estimate to Draft, so it can be changed again.
    /// </summary>
    /// <exception cref="NotFoundException">No estimate with this ID in the current company.</exception>
    /// <exception cref="EstimateStatusException">The estimate is already a Draft.</exception>
    Task<EstimateResponse> ReturnToDraftAsync(int id);

    /// <summary>
    /// Removes a work line together with its material lines.
    /// </summary>
    /// <exception cref="NotFoundException">No such estimate, or no such line in it.</exception>
    /// <exception cref="EstimateNotDraftException">The estimate is not Draft.</exception>
    Task<EstimateResponse> RemoveWorkLineAsync(int id, int lineId);
}
