using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.CatalogSections;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Tests.TestSupport;
using static SmartEstimatePlatform.Api.Tests.TestSupport.CatalogRequests;

namespace SmartEstimatePlatform.Api.Tests.EstimateSectionTests;

/// <summary>
/// Estimate sections through the real API (FR-031–FR-033, spec 019 US2): the catalog section "A++ siena" (m²) with
/// Tašelis × 1, Pelių barjeras × 0,4 and Gipso plokštės montavimas × 1, each work with labour and one material.
/// </summary>
public sealed class EstimateSectionEndpointsTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task AddSection_CopiesItsWorksWithSectionQuantityTimesCoefficient()
    {
        var (company, catalog, estimate) = await ArrangeAsync();

        var result = await AddSectionAsync(company, estimate.Id, catalog.SectionId, 121.7m);

        var section = Assert.Single(result.Sections);
        Assert.Equal(("A++ siena", "m²", 121.7m), (section.Name, section.Unit, section.Quantity));
        Assert.Equal(
            [("Tašelis 28x45 mm", 121.7m), ("Pelių barjeras", 48.68m), ("Gipso plokštės montavimas", 121.7m)],
            result.WorkLines.Select(l => (l.WorkName, l.Quantity)));
        Assert.All(result.WorkLines, l => Assert.Equal(section.Id, l.SectionId));
        Assert.All(result.WorkLines, l => Assert.Single(l.MaterialLines));
        Assert.Equal(result.DirectCosts, section.Subtotal);
        Assert.Equal(0m, result.UnsectionedSubtotal);
    }

    [Fact]
    public async Task SectionTotal_EqualsTheSameWorksAddedOneByOne()
    {
        // SC-019-2.
        var (company, catalog, estimate) = await ArrangeAsync();
        var byHand = await company.CreateEstimateAsync("Rankiniu būdu");
        foreach (var (workId, quantity) in catalog.Works.Zip([121.7m, 48.68m, 121.7m]))
        {
            await company.Client.AddWorkLineAsync(byHand.Id, workId, quantity);
        }

        var fromSection = await AddSectionAsync(company, estimate.Id, catalog.SectionId, 121.7m);

        Assert.Equal((await company.Client.GetEstimateAsync(byHand.Id)).Total, fromSection.Total);
    }

    [Fact]
    public async Task ChangeSectionQuantity_RecalculatesCatalogLines_AndKeepsWorksAddedByHand()
    {
        var (company, catalog, estimate) = await ArrangeAsync();
        var added = await AddSectionAsync(company, estimate.Id, catalog.SectionId, 121.7m);
        var sectionId = added.Sections[0].Id;
        var barrier = added.WorkLines[1];
        await Ok(await company.Client.PatchAsJsonAsync($"/api/estimates/{estimate.Id}/lines/{barrier.Id}",
            new ChangeQuantityRequest { Quantity = 50m }));
        await Ok(await company.Client.PostAsJsonAsync($"/api/estimates/{estimate.Id}/lines",
            new AddWorkLineRequest { WorkId = catalog.Works[0], Quantity = 7m, SectionId = sectionId }));

        var changed = await Ok(await company.Client.PatchAsJsonAsync($"/api/estimates/{estimate.Id}/sections/{sectionId}",
            new ChangeSectionQuantityRequest { Quantity = 130m }));

        Assert.Equal(130m, changed.Sections[0].Quantity);
        Assert.Equal([130m, 52m, 130m, 7m], changed.WorkLines.Select(l => l.Quantity));
    }

    [Fact]
    public async Task RemoveSection_RemovesItsLines_AndKeepsTheOthers()
    {
        var (company, catalog, estimate) = await ArrangeAsync();
        var added = await AddSectionAsync(company, estimate.Id, catalog.SectionId, 10m);
        await company.Client.AddWorkLineAsync(estimate.Id, catalog.Works[2], 3m);

        var removed = await Ok(await company.Client.DeleteAsync($"/api/estimates/{estimate.Id}/sections/{added.Sections[0].Id}"));

        Assert.Empty(removed.Sections);
        var line = Assert.Single(removed.WorkLines);
        Assert.Equal((3m, (int?)null), (line.Quantity, line.SectionId));
        Assert.Equal(removed.DirectCosts, removed.UnsectionedSubtotal);
    }

    [Fact]
    public async Task AddWork_ToASectionOrToNone_AndAnotherEstimatesSectionIsNotFound()
    {
        var (company, catalog, estimate) = await ArrangeAsync();
        var added = await AddSectionAsync(company, estimate.Id, catalog.SectionId, 10m);
        var other = await company.CreateEstimateAsync("Kita");
        var otherSection = (await AddSectionAsync(company, other.Id, catalog.SectionId, 1m)).Sections[0].Id;

        var inSection = await Ok(await company.Client.PostAsJsonAsync($"/api/estimates/{estimate.Id}/lines",
            new AddWorkLineRequest { WorkId = catalog.Works[0], Quantity = 2m, SectionId = added.Sections[0].Id }));
        var wrongSection = await company.Client.PostAsJsonAsync($"/api/estimates/{estimate.Id}/lines",
            new AddWorkLineRequest { WorkId = catalog.Works[0], Quantity = 2m, SectionId = otherSection });

        Assert.Equal(added.Sections[0].Id, inSection.WorkLines[^1].SectionId);
        Assert.Equal(inSection.DirectCosts, inSection.Sections.Sum(s => s.Subtotal) + inSection.UnsectionedSubtotal);
        Assert.Equal(HttpStatusCode.NotFound, wrongSection.StatusCode);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1.2345")]
    public async Task InvalidSectionQuantity_IsRejected(string quantity)
    {
        var (company, catalog, estimate) = await ArrangeAsync();

        var response = await company.Client.PostAsJsonAsync($"/api/estimates/{estimate.Id}/sections", new AddSectionRequest
        {
            CatalogSectionId = catalog.SectionId,
            Quantity = decimal.Parse(quantity, System.Globalization.CultureInfo.InvariantCulture)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WorkQuantityThatRoundsToZero_IsRefusedAndAddsNothing()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var work = await company.CreateWorkAsync(WorkRequest("Smulkus darbas"));
        var section = await CreateSectionAsync(company, "Smulkus", [(work.Id, 0.0001m)]);
        var estimate = await company.CreateEstimateAsync();

        var response = await company.Client.PostAsJsonAsync($"/api/estimates/{estimate.Id}/sections",
            new AddSectionRequest { CatalogSectionId = section, Quantity = 0.1m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var unchanged = await company.Client.GetEstimateAsync(estimate.Id);
        Assert.Empty(unchanged.Sections);
        Assert.Empty(unchanged.WorkLines);
    }

    [Fact]
    public async Task CatalogChangesAfterAdding_DoNotReachTheEstimate()
    {
        // BR-010.
        var (company, catalog, estimate) = await ArrangeAsync();
        var before = await AddSectionAsync(company, estimate.Id, catalog.SectionId, 10m);

        await company.Client.PutAsJsonAsync($"/api/catalog-sections/{catalog.SectionId}", new CatalogSectionRequest
        {
            Name = "Pervadinta", Unit = "m", Lines = [new CatalogSectionLineRequest { WorkId = catalog.Works[0], Coefficient = 9m }]
        });
        await company.Client.DeleteAsync($"/api/catalog-sections/{catalog.SectionId}");

        var after = await company.Client.GetEstimateAsync(estimate.Id);
        Assert.Equal(("A++ siena", "m²"), (after.Sections[0].Name, after.Sections[0].Unit));
        Assert.Equal(before.WorkLines.Select(l => l.Quantity), after.WorkLines.Select(l => l.Quantity));
        Assert.Equal(before.Total, after.Total);
    }

    [Fact]
    public async Task ConfirmedEstimate_RefusesEverySectionChange()
    {
        var (company, catalog, estimate) = await ArrangeAsync();
        var sectionId = (await AddSectionAsync(company, estimate.Id, catalog.SectionId, 10m)).Sections[0].Id;
        (await company.Client.ConfirmEstimateAsync(estimate.Id)).EnsureSuccessStatusCode();

        var add = await company.Client.PostAsJsonAsync($"/api/estimates/{estimate.Id}/sections",
            new AddSectionRequest { CatalogSectionId = catalog.SectionId, Quantity = 1m });
        var change = await company.Client.PatchAsJsonAsync($"/api/estimates/{estimate.Id}/sections/{sectionId}",
            new ChangeSectionQuantityRequest { Quantity = 2m });
        var remove = await company.Client.DeleteAsync($"/api/estimates/{estimate.Id}/sections/{sectionId}");

        Assert.Equal([HttpStatusCode.Conflict, HttpStatusCode.Conflict, HttpStatusCode.Conflict],
            new[] { add.StatusCode, change.StatusCode, remove.StatusCode });
    }

    [Fact]
    public async Task ViewerIsForbidden_AndAnotherCompanyGetsNotFound()
    {
        var (company, catalog, estimate) = await ArrangeAsync();
        var sectionId = (await AddSectionAsync(company, estimate.Id, catalog.SectionId, 10m)).Sections[0].Id;
        var viewer = await _factory.CreateViewerClientAsync(company);
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");

        var viewerAdd = await viewer.PostAsJsonAsync($"/api/estimates/{estimate.Id}/sections",
            new AddSectionRequest { CatalogSectionId = catalog.SectionId, Quantity = 1m });
        var betaRemove = await beta.Client.DeleteAsync($"/api/estimates/{estimate.Id}/sections/{sectionId}");
        var betaUsesAlphaCatalog = await beta.Client.PostAsJsonAsync(
            $"/api/estimates/{(await beta.CreateEstimateAsync()).Id}/sections",
            new AddSectionRequest { CatalogSectionId = catalog.SectionId, Quantity = 1m });

        Assert.Equal(HttpStatusCode.Forbidden, viewerAdd.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, betaRemove.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, betaUsesAlphaCatalog.StatusCode);
    }

    private sealed record Catalog(int SectionId, int[] Works);

    private async Task<(LoggedInCompany Company, Catalog Catalog, EstimateResponse Estimate)> ArrangeAsync()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var timber = await company.CreateMaterialAsync("Kalibruota mediena", "m³", 485m);
        var screws = await company.CreateMaterialAsync("Medsraigčiai", "vnt.", 0.1m);
        var gypsum = await company.CreateMaterialAsync("Gipso plokštė", "m²", 4.5m);
        var batten = await company.CreateWorkAsync(WorkRequest("Tašelis 28x45 mm", "m²", (timber.Id, 0.004m, 0m)) with { LabourRate = 25m, LabourNorm = 0.042m });
        var barrier = await company.CreateWorkAsync(WorkRequest("Pelių barjeras", "m", (screws.Id, 10m, 0m)) with { LabourRate = 25m, LabourNorm = 0.11m });
        var board = await company.CreateWorkAsync(WorkRequest("Gipso plokštės montavimas", "m²", (gypsum.Id, 1.05m, 5m)) with { LabourRate = 35m, LabourNorm = 0.3m });
        var section = await CreateSectionAsync(company, "A++ siena", [(batten.Id, 1m), (barrier.Id, 0.4m), (board.Id, 1m)]);
        var estimate = await company.CreateEstimateAsync();
        return (company, new Catalog(section, [batten.Id, barrier.Id, board.Id]), estimate);
    }

    private static async Task<int> CreateSectionAsync(LoggedInCompany company, string name, (int WorkId, decimal Coefficient)[] lines)
    {
        var response = await company.Client.PostAsJsonAsync("/api/catalog-sections", new CatalogSectionRequest
        {
            Name = name,
            Unit = "m²",
            Lines = lines.Select(l => new CatalogSectionLineRequest { WorkId = l.WorkId, Coefficient = l.Coefficient }).ToList()
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CatalogSectionResponse>())!.Id;
    }

    private static async Task<EstimateResponse> AddSectionAsync(LoggedInCompany company, int estimateId, int sectionId, decimal quantity) =>
        await Ok(await company.Client.PostAsJsonAsync($"/api/estimates/{estimateId}/sections",
            new AddSectionRequest { CatalogSectionId = sectionId, Quantity = quantity }));

    private static async Task<EstimateResponse> Ok(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EstimateResponse>())!;
    }
}
