using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SmartEstimatePlatform.Api.Models.DTOs.Materials;
using SmartEstimatePlatform.Api.Tests.TestSupport;
using static SmartEstimatePlatform.Api.Tests.TestSupport.CatalogRequests;

namespace SmartEstimatePlatform.Api.Tests.MaterialServiceTests;

/// <summary>
/// A material used in a work's composition cannot be deleted, so no work refers to a missing material (004 US3).
/// </summary>
public sealed class MaterialDeleteInUseTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Delete_MaterialUsedInWorks_IsRejectedAndListsTheWorks()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var paint = await company.CreateMaterialAsync("Dažai", "l");
        await company.CreateWorkAsync(WorkRequest("Sienų dažymas", "m²", (paint.Id, 0.150m, 5), (paint.Id, 0.050m, 0)));
        await company.CreateWorkAsync(WorkRequest("Lubų dažymas", "m²", (paint.Id, 0.200m, 5)));

        var response = await company.Client.DeleteAsync($"/api/materials/{paint.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
        Assert.Equal("The material is used in works and cannot be deleted.", problem.Detail);
        var works = ((JsonElement)problem.Extensions["works"]!).EnumerateArray().Select(w => w.GetString());
        Assert.Equal(["Lubų dažymas", "Sienų dažymas"], works);
        Assert.Single((await company.Client.GetFromJsonAsync<List<MaterialResponse>>("/api/materials"))!);
    }

    [Fact]
    public async Task Delete_MaterialNoLongerUsed_Succeeds()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var paint = await company.CreateMaterialAsync("Dažai", "l");
        var work = await company.CreateWorkAsync(WorkRequest("Sienų dažymas", "m²", (paint.Id, 0.150m, 5)));
        await company.Client.DeleteAsync($"/api/works/{work.Id}");

        var response = await company.Client.DeleteAsync($"/api/materials/{paint.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
