using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.Materials;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.MaterialServiceTests;

/// <summary>
/// The material catalog contains prices, so Viewers can never read or change it.
/// </summary>
public sealed class MaterialAuthorizationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Viewer_IsForbiddenFromEveryMaterialEndpoint()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var created = await company.Client.PostAsJsonAsync("/api/materials",
            new MaterialRequest { Name = "Cementas", Unit = "kg", Price = 1 });
        var material = (await created.Content.ReadFromJsonAsync<MaterialResponse>())!;
        var viewer = await _factory.CreateViewerClientAsync(company);
        var request = new MaterialRequest { Name = "Smėlis", Unit = "kg", Price = 2 };

        var responses = new[]
        {
            await viewer.GetAsync("/api/materials"),
            await viewer.GetAsync($"/api/materials/{material.Id}"),
            await viewer.PostAsJsonAsync("/api/materials", request),
            await viewer.PutAsJsonAsync($"/api/materials/{material.Id}", request),
            await viewer.DeleteAsync($"/api/materials/{material.Id}")
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
        var unchanged = Assert.Single((await company.Client.GetFromJsonAsync<List<MaterialResponse>>("/api/materials"))!);
        Assert.Equal(("Cementas", 1m), (unchanged.Name, unchanged.Price));
    }

    [Fact]
    public async Task Viewer_CanReadTheUnits()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var viewer = await _factory.CreateViewerClientAsync(company);

        var response = await viewer.GetAsync("/api/units");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousRequest_IsUnauthorized()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/materials")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/units")).StatusCode);
    }
}
