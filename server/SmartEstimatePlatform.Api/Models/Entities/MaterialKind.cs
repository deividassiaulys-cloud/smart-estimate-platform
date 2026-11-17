namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// The fixed list of material kinds (FR-030). A Machinery material's cost is counted under Machinery instead of
/// Materials (BR-015); labour is never a material, it is always the work's own. Sent to the client as a number.
/// </summary>
public enum MaterialKind
{
    Material = 0,
    Machinery = 1
}
