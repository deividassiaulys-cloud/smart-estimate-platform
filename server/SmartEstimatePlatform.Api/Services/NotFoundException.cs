namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when a record does not exist for the current company. Another company's record is reported
/// exactly like one that does not exist at all, so its existence is never revealed.
/// </summary>
public class NotFoundException(string message = "The requested record was not found.") : Exception(message);
