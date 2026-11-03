namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when a change is attempted on an estimate that is not Draft (BR-013).
/// </summary>
public class EstimateNotDraftException()
    : Exception("The estimate is confirmed and cannot be changed.");
