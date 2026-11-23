using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Tests.TestSupport;
using static SmartEstimatePlatform.Api.Tests.TestSupport.CatalogRequests;

namespace SmartEstimatePlatform.Api.Tests.EstimateSectionTests;

/// <summary>
/// Another company's estimate sections are not found, whatever the request (feature 021, F2).
/// </summary>
public sealed class EstimateSectionIsolationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task AnotherCompanysEstimate_SectionsAreNotFound()
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var (estimateId, sectionId) = await EstimateSectionAuthorizationTests.EstimateWithSectionAsync(alpha);
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        var betaWork = await beta.CreateWorkAsync(WorkRequest("Beta darbas"));
        var betaCatalogSection = await beta.CreateCatalogSectionAsync(CatalogSectionRequest("Beta skyrius", (betaWork.Id, 1m)));

        var add = await beta.Client.PostAsJsonAsync($"/api/estimates/{estimateId}/sections",
            new AddSectionRequest { CatalogSectionId = betaCatalogSection.Id, Quantity = 1m });
        var change = await beta.Client.PatchAsJsonAsync($"/api/estimates/{estimateId}/sections/{sectionId}",
            new ChangeSectionQuantityRequest { Quantity = 99m });
        var remove = await beta.Client.DeleteAsync($"/api/estimates/{estimateId}/sections/{sectionId}");

        Assert.All([add, change, remove], response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
        var unchanged = Assert.Single((await alpha.Client.GetEstimateAsync(estimateId)).Sections);
        Assert.Equal(("A++ siena", 10m), (unchanged.Name, unchanged.Quantity));
    }
}
