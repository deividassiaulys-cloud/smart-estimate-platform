using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CompanyIsolationTests;

/// <summary>
/// A Manager can never see or change another company's materials; they are reported as not found.
/// </summary>
public sealed class MaterialIsolationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task AnotherCompanysMaterial_IsNotListedAndCannotBeReadChangedOrDeleted()
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        var created = await alpha.Client.PostAsJsonAsync("/api/materials",
            new MaterialRequest { Name = "Cementas", Unit = "kg", Price = 1 });
        var alphaMaterial = (await created.Content.ReadFromJsonAsync<MaterialResponse>())!;

        var betaList = await beta.Client.GetFromJsonAsync<List<MaterialResponse>>("/api/materials?search=Cem");
        var get = await beta.Client.GetAsync($"/api/materials/{alphaMaterial.Id}");
        var update = await beta.Client.PutAsJsonAsync($"/api/materials/{alphaMaterial.Id}",
            new MaterialRequest { Name = "Pakeista", Unit = "kg", Price = 0 });
        var delete = await beta.Client.DeleteAsync($"/api/materials/{alphaMaterial.Id}");

        Assert.Empty(betaList!);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        // Alpha's material is untouched.
        var alphaList = await alpha.Client.GetFromJsonAsync<List<MaterialResponse>>("/api/materials");
        var unchanged = Assert.Single(alphaList!);
        Assert.Equal(("Cementas", 1m), (unchanged.Name, unchanged.Price));
    }

    [Fact]
    public async Task SameNameAndUnit_IsAllowedInDifferentCompanies()
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        var request = new MaterialRequest { Name = "Cementas", Unit = "kg", Price = 1 };

        await alpha.Client.PostAsJsonAsync("/api/materials", request);
        var response = await beta.Client.PostAsJsonAsync("/api/materials", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
