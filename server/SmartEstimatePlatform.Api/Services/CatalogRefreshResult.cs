namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// What a catalog refresh changed in an estimate, and which lines it could not match.
/// </summary>
public sealed record CatalogRefreshResult(IReadOnlyList<RefreshChange> Changes, IReadOnlyList<RefreshMiss> NotFound);
