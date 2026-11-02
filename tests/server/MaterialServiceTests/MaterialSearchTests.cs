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
