namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// One value a catalog refresh changes: on a work line (no material name) or on one of its material lines.
/// </summary>
public sealed record RefreshChange(string WorkName, string? MaterialName, RefreshedField Field, decimal OldValue, decimal NewValue);
