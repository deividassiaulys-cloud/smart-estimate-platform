using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using static SmartEstimatePlatform.Api.Tests.TestSupport.CatalogRequests;

namespace SmartEstimatePlatform.Api.Tests.TestSupport;

/// <summary>
/// Shortcuts for building the spec's worked example through the real API in tests.
/// </summary>
public static class EstimateRequests
{
    /// <summary>
    /// Creates the worked example's materials and the work "Siena A++" (m², 15 €/h, 0.8 h/m²), returning the work's ID.
    /// </summary>
    public static async Task<int> CreateWallWorkAsync(this LoggedInCompany company)
    {
        var board = await company.CreateMaterialAsync("Gipso plokštė", "m²", 4.50m);
        var screws = await company.CreateMaterialAsync("Varžtai", "vnt.", 0.02m);
        var film = await company.CreateMaterialAsync("Plėvelė", "m²", 1.10m);
        var work = await company.CreateWorkAsync(WorkRequest("Siena A++", "m²", (board.Id, 2m, 10m), (screws.Id, 25m, 5m), (film.Id, 1m, 5m))
            with { LabourRate = 15m, LabourNorm = 0.8m });
        return work.Id;
    }

    /// <summary>
    /// Creates a project with a Draft estimate in it, returning the estimate.
    /// </summary>
    public static async Task<EstimateResponse> CreateEstimateAsync(this LoggedInCompany company, string name = "Pradinė sąmata")
    {
        var project = await company.Client.CreateProjectAsync("Buto remontas");
        var response = await company.Client.PostAsJsonAsync($"/api/projects/{project.Id}/estimates", new CreateEstimateRequest { Name = name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EstimateResponse>())!;
    }

    /// <summary>
    /// Adds a work to an estimate and returns the raw response.
    /// </summary>
    public static Task<HttpResponseMessage> PostWorkLineAsync(this HttpClient client, int estimateId, int workId, decimal quantity) =>
        client.PostAsJsonAsync($"/api/estimates/{estimateId}/lines", new AddWorkLineRequest { WorkId = workId, Quantity = quantity });

    /// <summary>
    /// Adds a work to an estimate and returns the recalculated estimate; fails the test if the API rejects it.
    /// </summary>
    public static async Task<EstimateResponse> AddWorkLineAsync(this HttpClient client, int estimateId, int workId, decimal quantity)
    {
        var response = await client.PostWorkLineAsync(estimateId, workId, quantity);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EstimateResponse>())!;
    }

    /// <summary>
    /// Sets the overhead, profit and VAT percentages and returns the raw response.
    /// </summary>
    public static Task<HttpResponseMessage> PatchPercentagesAsync(this HttpClient client, int estimateId, decimal overhead, decimal profit, decimal vat) =>
        client.PatchAsJsonAsync($"/api/estimates/{estimateId}/percentages",
            new ChangePercentagesRequest { OverheadPercent = overhead, ProfitPercent = profit, VatPercent = vat });

    /// <summary>
    /// Confirms an estimate and returns the raw response.
    /// </summary>
    public static Task<HttpResponseMessage> ConfirmEstimateAsync(this HttpClient client, int estimateId) =>
        client.PatchAsync($"/api/estimates/{estimateId}/confirm", null);

    /// <summary>
    /// Returns an estimate to Draft and returns the raw response.
    /// </summary>
    public static Task<HttpResponseMessage> ReturnEstimateToDraftAsync(this HttpClient client, int estimateId) =>
        client.PatchAsync($"/api/estimates/{estimateId}/draft", null);

    /// <summary>
    /// Reads an estimate.
    /// </summary>
    public static async Task<EstimateResponse> GetEstimateAsync(this HttpClient client, int estimateId) =>
        (await client.GetFromJsonAsync<EstimateResponse>($"/api/estimates/{estimateId}"))!;
}
