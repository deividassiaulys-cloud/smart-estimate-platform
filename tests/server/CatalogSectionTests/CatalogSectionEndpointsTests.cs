using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.CatalogSections;
using SmartEstimatePlatform.Api.Tests.TestSupport;
using static SmartEstimatePlatform.Api.Tests.TestSupport.CatalogRequests;

namespace SmartEstimatePlatform.Api.Tests.CatalogSectionTests;

/// <summary>
/// The catalog sections (FR-030, spec 019 US1) through the real API: "A++ siena" (m²) made of Tašelis × 1,
/// Pelių barjeras × 0,4 and Gipso plokštės montavimas × 1.
/// </summary>
public sealed class CatalogSectionEndpointsTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Create_SavesTheWorksInOrder_AndGetReturnsThem()
    {
        var (company, works) = await ArrangeAsync();

        var created = await CreateAsync(company, Wall(works));
        var saved = await company.Client.GetFromJsonAsync<CatalogSectionResponse>($"/api/catalog-sections/{created.Id}");

        Assert.Equal(("A++ siena", "m²"), (saved!.Name, saved.Unit));
        Assert.Equal(
            [("Tašelis 28x45 mm", "m²", 1m), ("Pelių barjeras", "m", 0.4m), ("Gipso plokštės montavimas", "m²", 1m)],
            saved.Lines.Select(l => (l.WorkName, l.WorkUnit, l.Coefficient)));
    }

    [Fact]
    public async Task Update_RenamesAndReplacesTheLines_AndTheSameWorkMayRepeat()
    {
        var (company, works) = await ArrangeAsync();
        var created = await CreateAsync(company, Wall(works));

        var response = await company.Client.PutAsJsonAsync($"/api/catalog-sections/{created.Id}", new CatalogSectionRequest
        {
            Name = "A++ siena 2 sl.",
            Unit = "m²",
            Lines = [Line(works[2], 1m), Line(works[2], 1m)]
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = (await response.Content.ReadFromJsonAsync<CatalogSectionResponse>())!;
        Assert.Equal("A++ siena 2 sl.", saved.Name);
        Assert.Equal([works[2], works[2]], saved.Lines.Select(l => l.WorkId));
    }

    [Fact]
    public async Task Search_IgnoresLetterCase()
    {
        var (company, works) = await ArrangeAsync();
        await CreateAsync(company, Wall(works));
        await CreateAsync(company, Wall(works) with { Name = "Stogas" });

        var found = await company.Client.GetFromJsonAsync<List<CatalogSectionResponse>>("/api/catalog-sections?search=SIENA");

        Assert.Equal(["A++ siena"], found!.Select(s => s.Name));
    }

    [Fact]
    public async Task Delete_RemovesTheSection()
    {
        var (company, works) = await ArrangeAsync();
        var created = await CreateAsync(company, Wall(works));

        var response = await company.Client.DeleteAsync($"/api/catalog-sections/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty((await company.Client.GetFromJsonAsync<List<CatalogSectionResponse>>("/api/catalog-sections"))!);
    }

    [Fact]
    public async Task SameNameAndUnitInAnyLetterCase_IsADuplicate()
    {
        var (company, works) = await ArrangeAsync();
        await CreateAsync(company, Wall(works));

        var response = await company.Client.PostAsJsonAsync("/api/catalog-sections", Wall(works) with { Name = "a++ SIENA" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("0.0000001")]
    public async Task InvalidCoefficient_IsRejected(string coefficient)
    {
        var (company, works) = await ArrangeAsync();

        var response = await company.Client.PostAsJsonAsync("/api/catalog-sections", Wall(works) with
        {
            Lines = [Line(works[0], decimal.Parse(coefficient, System.Globalization.CultureInfo.InvariantCulture))]
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SectionWithoutWorksOrWithABadUnit_IsRejected()
    {
        var (company, works) = await ArrangeAsync();

        var empty = await company.Client.PostAsJsonAsync("/api/catalog-sections", Wall(works) with { Lines = [] });
        var badUnit = await company.Client.PostAsJsonAsync("/api/catalog-sections", Wall(works) with { Unit = "m2" });

        Assert.Equal((HttpStatusCode.BadRequest, HttpStatusCode.BadRequest), (empty.StatusCode, badUnit.StatusCode));
    }

    [Fact]
    public async Task AnotherCompanysWork_IsRejectedAndSavesNothing()
    {
        var (company, works) = await ArrangeAsync();
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        var betaWork = await beta.CreateWorkAsync(WorkRequest("Betos darbas"));

        var response = await company.Client.PostAsJsonAsync("/api/catalog-sections", Wall(works) with
        {
            Lines = [Line(works[0], 1m), Line(betaWork.Id, 1m)]
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty((await company.Client.GetFromJsonAsync<List<CatalogSectionResponse>>("/api/catalog-sections"))!);
    }

    [Fact]
    public async Task AnotherCompanysSection_IsNotFound_AndAViewerIsForbidden()
    {
        var (company, works) = await ArrangeAsync();
        var created = await CreateAsync(company, Wall(works));
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        var viewer = await _factory.CreateViewerClientAsync(company);

        Assert.Equal(HttpStatusCode.NotFound, (await beta.Client.GetAsync($"/api/catalog-sections/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await beta.Client.DeleteAsync($"/api/catalog-sections/{created.Id}")).StatusCode);
        Assert.Empty((await beta.Client.GetFromJsonAsync<List<CatalogSectionResponse>>("/api/catalog-sections"))!);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync("/api/catalog-sections")).StatusCode);
    }

    [Fact]
    public async Task WorkUsedInASection_CannotBeDeletedUntilTheSectionIsGone()
    {
        var (company, works) = await ArrangeAsync();
        var created = await CreateAsync(company, Wall(works));

        var refused = await company.Client.DeleteAsync($"/api/works/{works[1]}");
        await company.Client.DeleteAsync($"/api/catalog-sections/{created.Id}");
        var allowed = await company.Client.DeleteAsync($"/api/works/{works[1]}");

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Contains("A++ siena", await refused.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
    }

    private async Task<(LoggedInCompany Company, int[] Works)> ArrangeAsync()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var batten = await company.CreateWorkAsync(WorkRequest("Tašelis 28x45 mm", "m²"));
        var barrier = await company.CreateWorkAsync(WorkRequest("Pelių barjeras", "m"));
        var gypsum = await company.CreateWorkAsync(WorkRequest("Gipso plokštės montavimas", "m²"));
        return (company, [batten.Id, barrier.Id, gypsum.Id]);
    }

    private static CatalogSectionRequest Wall(int[] works) => new()
    {
        Name = "A++ siena",
        Unit = "m²",
        Lines = [Line(works[0], 1m), Line(works[1], 0.4m), Line(works[2], 1m)]
    };

    private static CatalogSectionLineRequest Line(int workId, decimal coefficient) =>
        new() { WorkId = workId, Coefficient = coefficient };

    private static async Task<CatalogSectionResponse> CreateAsync(LoggedInCompany company, CatalogSectionRequest request)
    {
        var response = await company.Client.PostAsJsonAsync("/api/catalog-sections", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CatalogSectionResponse>())!;
    }
}
