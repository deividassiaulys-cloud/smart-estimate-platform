namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// A copied value of an estimate line that a catalog refresh can update (spec 015). For <see cref="Kind"/>, the old
/// and new values are <see cref="Models.Entities.MaterialKind"/> numbers (feature 017).
/// </summary>
public enum RefreshedField
{
    LabourRate,
    LabourNorm,
    Price,
    ConsumptionNorm,
    WastePercent,
    Kind
}
