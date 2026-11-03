namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// Whether an estimate may still be changed. A new estimate is always Draft (BR-012).
/// </summary>
public enum EstimateStatus
{
    /// <summary>
    /// Being prepared; works, quantities and percentages may change.
    /// </summary>
    Draft,

    /// <summary>
    /// Final; nothing may change until it is returned to Draft (BR-013, feature 008).
    /// </summary>
    Confirmed
}
