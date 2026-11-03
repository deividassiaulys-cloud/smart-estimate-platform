namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when a status change is not allowed from the estimate's current status, such as confirming an
/// estimate that is already Confirmed.
/// </summary>
public class EstimateStatusException(string message) : Exception(message);
