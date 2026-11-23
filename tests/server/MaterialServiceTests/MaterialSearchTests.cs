using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.Materials;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.MaterialServiceTests;

/// <summary>
/// Searching the material catalog by part of the name, through the real API.
/// </summary>
public sealed class MaterialSearchTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("plyt")]
    [InlineData("PLYT")]
    [InlineData("  Plyt ")]
    public async Task Search_MatchesPartOfTheNameInAnyLetterCase_SortedByName(string search)
    {
        var company = await SeedAsync();

        var names = await SearchAsync(company, search);

        Assert.Equal(["Gipso plytos", "Keraminės plytelės"], names);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_Empty_ReturnsEveryMaterialSortedByName(string? search)
    {
        var company = await SeedAsync();

        var names = await SearchAsync(company, search);

        Assert.Equal(["cementas", "Gipso plytos", "Keraminės plytelės", "Smėlis 0/4"], names);
    }

    [Fact]
    public async Task Search_NoMatches_ReturnsAnEmptyList()
    {
        var company = await SeedAsync();

        Assert.Empty(await SearchAsync(company, "medis"));
    }

    [Fact]
    public async Task Search_PercentSign_MatchesLiterally()
    {
        var company = await SeedAsync();

        Assert.Empty(await SearchAsync(company, "%"));
    }

    [Theory]
    [InlineData("/api/materials")]
    [InlineData("/api/works")]
    [InlineData("/api/catalog-sections")]
    public async Task Search_SqlLikeText_IsTreatedAsData(string catalog)
    {
        // Each catalog holds one record named "Cementas" in Alpha; Beta has none. SQL in the search text must be
        // matched as plain text, so it finds nothing and never reveals Alpha's record (feature 021, F2).
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var material = await alpha.CreateMaterialAsync("Cementas");
        var work = await alpha.CreateWorkAsync(CatalogRequests.WorkRequest("Cementas", "m²", (material.Id, 1m, 0m)));
        await alpha.CreateCatalogSectionAsync(CatalogRequests.CatalogSectionRequest("Cementas", (work.Id, 1m)));
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");

        foreach (var search in new[] { "' OR 1=1 --", "%' OR '1'='1", "Cementas'; DROP TABLE Materials; --" })
        {
            Assert.Empty(await NamesAsync(alpha, catalog, search));
            Assert.Empty(await NamesAsync(beta, catalog, search));
        }

        Assert.Equal(["Cementas"], await NamesAsync(alpha, catalog, "Cem"));
        Assert.Empty(await NamesAsync(beta, catalog, "Cem"));
    }

    [Theory]
    [InlineData("/api/materials")]
    [InlineData("/api/works")]
    [InlineData("/api/catalog-sections")]
    public async Task Search_OverTheLimit_Returns400(string catalog)
    {
        // A search text is at most 200 characters (feature 021, F9).
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var atLimit = await company.Client.GetAsync($"{catalog}?search={new string('a', 200)}");
        var overLimit = await company.Client.GetAsync($"{catalog}?search={new string('a', 201)}");

        Assert.Equal((HttpStatusCode.OK, HttpStatusCode.BadRequest), (atLimit.StatusCode, overLimit.StatusCode));
    }

    private static async Task<List<string>> NamesAsync(LoggedInCompany company, string catalog, string search)
    {
        var items = await company.Client.GetFromJsonAsync<List<NamedItem>>($"{catalog}?search={Uri.EscapeDataString(search)}");
        return items!.Select(i => i.Name).ToList();
    }

    private sealed record NamedItem(string Name);

    private async Task<LoggedInCompany> SeedAsync()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        foreach (var name in new[] { "Smėlis 0/4", "Keraminės plytelės", "cementas", "Gipso plytos" })
        {
            var response = await company.Client.PostAsJsonAsync("/api/materials",
                new MaterialRequest { Name = name, Unit = "kg", Price = 1 });
            response.EnsureSuccessStatusCode();
        }

        return company;
    }

    private static async Task<List<string>> SearchAsync(LoggedInCompany company, string? search)
    {
        var path = search is null ? "/api/materials" : $"/api/materials?search={Uri.EscapeDataString(search)}";
        var materials = await company.Client.GetFromJsonAsync<List<MaterialResponse>>(path);
        return materials!.Select(m => m.Name).ToList();
    }
}
