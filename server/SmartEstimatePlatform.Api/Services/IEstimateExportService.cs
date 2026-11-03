namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Exports an estimate to Excel with the same data the Estimate Editor shows (FR-027).
/// </summary>
public interface IEstimateExportService
{
    /// <summary>
    /// Builds the .xlsx file of an estimate, Draft or Confirmed.
    /// </summary>
    /// <exception cref="NotFoundException">No estimate with this ID in the current company.</exception>
    Task<ExportFile> ExportAsync(int id);
}
