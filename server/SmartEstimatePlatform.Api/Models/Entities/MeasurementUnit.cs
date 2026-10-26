namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// The fixed list of measurement units (BR-008). Users cannot add their own; services validate
/// against this list and the frontend loads it from GET /api/units.
/// </summary>
public static class MeasurementUnit
{
    /// <summary>
    /// Every allowed unit, in the order shown to users.
    /// </summary>
    public static readonly IReadOnlyList<string> All = ["vnt.", "m", "m²", "m³", "kg", "t", "l", "val.", "kompl."];

    /// <summary>
    /// Whether the unit is in the fixed list, compared exactly as written.
    /// </summary>
    public static bool IsValid(string? unit) => unit is not null && All.Contains(unit);
}
