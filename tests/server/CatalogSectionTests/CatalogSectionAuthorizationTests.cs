using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.CatalogSections;
using SmartEstimatePlatform.Api.Tests.TestSupport;
using static SmartEstimatePlatform.Api.Tests.TestSupport.CatalogRequests;

namespace SmartEstimatePlatform.Api.Tests.CatalogSectionTests;

/// <summary>
/// Catalog sections are Manager-only: a Viewer of the same company is refused everywhere (feature 021, F2).
/// </summary>
public sealed class CatalogSectionAuthorizationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Viewer_IsForbiddenFromEveryCatalogSectionEndpoint()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var work = await company.CreateWorkAsync(WorkRequest("Gipso plokštės montavimas"));
        var section = await company.CreateCatalogSectionAsync(CatalogSectionRequest("A++ siena", (work.Id, 1m)));
        var viewer = await _factory.CreateViewerClientAsync(company);
        var request = CatalogSectionRequest("Pakeista", (work.Id, 2m));

        var responses = new[]
        {
            await viewer.GetAsync("/api/catalog-sections"),
            await viewer.GetAsync($"/api/catalog-sections/{section.Id}"),
            await viewer.PostAsJsonAsync("/api/catalog-sections", request),
            await viewer.PutAsJsonAsync($"/api/catalog-sections/{section.Id}", request),
            await viewer.DeleteAsync($"/api/catalog-sections/{section.Id}")
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
        var unchanged = Assert.Single((await company.Client.GetFromJsonAsync<List<CatalogSectionResponse>>("/api/catalog-sections"))!);
        Assert.Equal("A++ siena", unchanged.Name);
    }
}
