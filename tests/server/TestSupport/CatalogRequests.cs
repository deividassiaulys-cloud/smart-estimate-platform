using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.Materials;
using SmartEstimatePlatform.Api.Models.DTOs.Works;

namespace SmartEstimatePlatform.Api.Tests.TestSupport;

/// <summary>
/// Shortcuts for creating catalog records through the real API in tests.
/// </summary>
public static class CatalogRequests
{
    /// <summary>
    /// Creates a material and returns it; fails the test if the API rejects it.
    /// </summary>
    public static async Task<MaterialResponse> CreateMaterialAsync(
        this LoggedInCompany company, string name, string unit = "kg", decimal price = 1)
    {
        var response = await company.Client.PostAsJsonAsync("/api/materials",
            new MaterialRequest { Name = name, Unit = unit, Price = price });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MaterialResponse>())!;
    }

    /// <summary>
    /// A valid work request with the given lines (material ID, consumption norm, waste %).
    /// </summary>
    public static WorkRequest WorkRequest(
        string name, string unit = "m²", params (int MaterialId, decimal Norm, decimal Waste)[] lines) => new()
    {
        Name = name,
        Unit = unit,
        LabourRate = 18.50m,
        LabourNorm = 0.250m,
        Lines = lines.Select(l => new WorkMaterialLineRequest
        {
            MaterialId = l.MaterialId,
            ConsumptionNorm = l.Norm,
            WastePercent = l.Waste
        }).ToList()
    };

    /// <summary>
    /// Posts a work and returns the raw response, so tests can check rejected requests too.
    /// </summary>
    public static Task<HttpResponseMessage> PostWorkAsync(this LoggedInCompany company, WorkRequest request) =>
        company.Client.PostAsJsonAsync("/api/works", request);

    /// <summary>
    /// Creates a work and returns it; fails the test if the API rejects it.
    /// </summary>
    public static async Task<WorkResponse> CreateWorkAsync(this LoggedInCompany company, WorkRequest request)
    {
        var response = await company.PostWorkAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkResponse>())!;
    }

    /// <summary>
    /// Lists the company's works, optionally filtered by a search text.
    /// </summary>
    public static async Task<List<WorkResponse>> ListWorksAsync(this LoggedInCompany company, string? search = null) =>
        (await company.Client.GetFromJsonAsync<List<WorkResponse>>(
            search is null ? "/api/works" : $"/api/works?search={Uri.EscapeDataString(search)}"))!;
}
