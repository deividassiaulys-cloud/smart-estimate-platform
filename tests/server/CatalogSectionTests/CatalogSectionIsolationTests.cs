using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.CatalogSections;
using SmartEstimatePlatform.Api.Tests.TestSupport;
using static SmartEstimatePlatform.Api.Tests.TestSupport.CatalogRequests;

namespace SmartEstimatePlatform.Api.Tests.CatalogSectionTests;

/// <summary>
/// Another company's catalog section cannot be changed (feature 021, F2). Reading and deleting it are covered by
/// CatalogSectionEndpointsTests.AnotherCompanysSection_IsNotFound_AndAViewerIsForbidden.
/// </summary>
public sealed class CatalogSectionIsolationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task AnotherCompanysSection_CannotBeUpdated()
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var alphaWork = await alpha.CreateWorkAsync(WorkRequest("Gipso plokštės montavimas"));
        var section = await alpha.CreateCatalogSectionAsync(CatalogSectionRequest("A++ siena", (alphaWork.Id, 1m)));
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        var betaWork = await beta.CreateWorkAsync(WorkRequest("Beta darbas"));

        var update = await beta.Client.PutAsJsonAsync($"/api/catalog-sections/{section.Id}",
            CatalogSectionRequest("Pakeista", (betaWork.Id, 2m)));

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        var unchanged = await alpha.Client.GetFromJsonAsync<CatalogSectionResponse>($"/api/catalog-sections/{section.Id}");
        Assert.Equal(("A++ siena", alphaWork.Id, 1m), (unchanged!.Name, Assert.Single(unchanged.Lines).WorkId, unchanged.Lines[0].Coefficient));
    }
}
