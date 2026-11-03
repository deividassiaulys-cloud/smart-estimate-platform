using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.Materials;
using SmartEstimatePlatform.Api.Models.DTOs.Works;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.EstimateServiceTests;

/// <summary>
/// Estimate lines hold copies of the catalog data, so later catalog changes or deletions never affect them (BR-010).
/// </summary>
public sealed class EstimateCopyIsolationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task CatalogChangesAndDeletions_DoNotChangeAnExistingEstimate()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var workId = await company.CreateWallWorkAsync();
        var estimate = await company.CreateEstimateAsync();
        var before = await company.Client.AddWorkLineAsync(estimate.Id, workId, 35m);

        // Change the work's labour rate and a material's price, then delete the work and every material.
        var work = (await company.Client.GetFromJsonAsync<WorkResponse>($"/api/works/{workId}"))!;
        await company.Client.PutAsJsonAsync($"/api/works/{workId}", new WorkRequest
        {
            Name = work.Name, Unit = work.Unit, LabourRate = 99m, LabourNorm = work.LabourNorm, Lines = []
        });
        var materials = (await company.Client.GetFromJsonAsync<List<MaterialResponse>>("/api/materials"))!;
        var film = materials.Single(m => m.Name == "Plėvelė");
        await company.Client.PutAsJsonAsync($"/api/materials/{film.Id}", new MaterialRequest { Name = film.Name, Unit = film.Unit, Price = 50m });
        (await company.Client.DeleteAsync($"/api/works/{workId}")).EnsureSuccessStatusCode();
        foreach (var material in materials)
        {
            (await company.Client.DeleteAsync($"/api/materials/{material.Id}")).EnsureSuccessStatusCode();
        }

        var after = await company.Client.GetEstimateAsync(estimate.Id);

        Assert.Equal(before.Total, after.Total);
        var line = Assert.Single(after.WorkLines);
        Assert.Equal((15m, 420.00m), (line.LabourRate, line.LabourCost));
        Assert.Equal([4.50m, 0.02m, 1.10m], line.MaterialLines.Select(m => m.Price));
    }
}
