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
    /// Lists every estimate of a project (Draft and Confirmed alike) by name, with nothing but ID and name,
    /// for any user of the company.
    /// </summary>
    /// <exception cref="NotFoundException">No project with this ID in the current company.</exception>
    Task<List<EstimateNameResponse>> ListNamesForProjectAsync(int projectId);

    /// <summary>
    /// Returns an estimate's name and its works' and materials' names, for any user of the company.
    /// </summary>
    /// <exception cref="NotFoundException">No estimate with this ID in the current company.</exception>
    Task<EstimateNamesResponse> GetNamesAsync(int id);

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

    /// <summary>
    /// Changes one work line's labour rate in this estimate only; the catalog work is not changed (BR-010).
    /// </summary>
    /// <exception cref="NotFoundException">No such estimate, or no such line in it.</exception>
    /// <exception cref="EstimateNotDraftException">The estimate is not Draft.</exception>
    Task<EstimateResponse> ChangeLabourRateAsync(int id, int lineId, ChangeLabourRateRequest request);

    /// <summary>
    /// Changes one material line's price in this estimate only; the catalog material is not changed (BR-010).
    /// </summary>
    /// <exception cref="NotFoundException">No such estimate, work line or material line in it.</exception>
    /// <exception cref="EstimateNotDraftException">The estimate is not Draft.</exception>
    Task<EstimateResponse> ChangeMaterialPriceAsync(int id, int lineId, int materialLineId, ChangePriceRequest request);

    /// <summary>
    /// Replaces a material line's material with a catalog material of the same unit, copying its name, unit and
    /// current price and keeping the line's norm and waste.
    /// </summary>
    /// <exception cref="NotFoundException">No such estimate, work line or material line in it.</exception>
    /// <exception cref="EstimateNotDraftException">The estimate is not Draft.</exception>
    /// <exception cref="UnknownMaterialException">The material is not in the current company's catalog.</exception>
    /// <exception cref="MaterialUnitMismatchException">The material has another unit than the line.</exception>
    Task<EstimateResponse> ReplaceMaterialAsync(int id, int lineId, int materialLineId, ReplaceMaterialRequest request);

    /// <summary>
    /// Removes one material line from a work line; the work line and its other material lines stay.
    /// </summary>
    /// <exception cref="NotFoundException">No such estimate, work line or material line in it.</exception>
    /// <exception cref="EstimateNotDraftException">The estimate is not Draft.</exception>
    Task<EstimateResponse> RemoveMaterialLineAsync(int id, int lineId, int materialLineId);
}
