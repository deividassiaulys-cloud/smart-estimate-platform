using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Tests.TestSupport;
using static SmartEstimatePlatform.Api.Tests.TestSupport.CatalogRequests;

namespace SmartEstimatePlatform.Api.Tests.EstimateSectionTests;

/// <summary>
/// A Viewer cannot change or remove an estimate section (feature 021, F2). Adding one is covered by
/// EstimateSectionEndpointsTests.ViewerIsForbidden_AndAnotherCompanyGetsNotFound.
/// </summary>
public sealed class EstimateSectionAuthorizationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Viewer_CannotChangeOrRemoveASection()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var (estimateId, sectionId) = await EstimateWithSectionAsync(company);
        var viewer = await _factory.CreateViewerClientAsync(company);

        var change = await viewer.PatchAsJsonAsync($"/api/estimates/{estimateId}/sections/{sectionId}",
            new ChangeSectionQuantityRequest { Quantity = 99m });
        var remove = await viewer.DeleteAsync($"/api/estimates/{estimateId}/sections/{sectionId}");

        Assert.Equal((HttpStatusCode.Forbidden, HttpStatusCode.Forbidden), (change.StatusCode, remove.StatusCode));
        var unchanged = Assert.Single((await company.Client.GetEstimateAsync(estimateId)).Sections);
        Assert.Equal(10m, unchanged.Quantity);
    }

    /// <summary>
    /// A Draft estimate holding one section "A++ siena" with quantity 10, built from a one-work catalog section.
    /// </summary>
    internal static async Task<(int EstimateId, int SectionId)> EstimateWithSectionAsync(LoggedInCompany company)
    {
        var work = await company.CreateWorkAsync(WorkRequest("Gipso plokštės montavimas"));
        var catalogSection = await company.CreateCatalogSectionAsync(CatalogSectionRequest("A++ siena", (work.Id, 1m)));
        var estimate = await company.CreateEstimateAsync();
        var response = await company.Client.PostAsJsonAsync($"/api/estimates/{estimate.Id}/sections",
            new AddSectionRequest { CatalogSectionId = catalogSection.Id, Quantity = 10m });
        response.EnsureSuccessStatusCode();
        var withSection = (await response.Content.ReadFromJsonAsync<EstimateResponse>())!;
        return (estimate.Id, Assert.Single(withSection.Sections).Id);
    }
}
