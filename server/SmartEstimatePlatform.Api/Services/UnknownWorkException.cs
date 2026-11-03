namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when a work added to an estimate is not in the current company's catalog.
/// Another company's work is reported exactly like one that does not exist.
/// </summary>
public class UnknownWorkException(int workId)
    : Exception($"Work {workId} is not in the catalog.");
