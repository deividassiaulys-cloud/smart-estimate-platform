using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.Works;
using SmartEstimatePlatform.Api.Tests.TestSupport;
using static SmartEstimatePlatform.Api.Tests.TestSupport.CatalogRequests;

namespace SmartEstimatePlatform.Api.Tests.WorkServiceTests;

/// <summary>
/// Integration tests for creating, editing and deleting works with their composition, through the real API.
/// </summary>
public sealed class WorkEndpointsTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Create_SameMaterialOnTwoLines_IsSavedWithBothLines()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var paint = await company.CreateMaterialAsync("Dažai", "l", 9.99m);

        var response = await company.PostWorkAsync(WorkRequest("  Sienų dažymas  ", "m²", (paint.Id, 0.150m, 5), (paint.Id, 0.050m, 0)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var work = Assert.Single(await company.ListWorksAsync());
        Assert.Equal(("Sienų dažymas", "m²", 18.50m, 0.250m), (work.Name, work.Unit, work.LabourRate, work.LabourNorm));
        Assert.Equal(
            [(paint.Id, "Dažai", "l", 0.150m, 5m), (paint.Id, "Dažai", "l", 0.050m, 0m)],
            work.Lines.Select(l => (l.MaterialId, l.MaterialName, l.MaterialUnit, l.ConsumptionNorm, l.WastePercent)));
    }

    [Fact]
    public async Task Create_WithoutLines_IsAccepted()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var response = await company.PostWorkAsync(WorkRequest("Konsultacija", "val."));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Empty(Assert.Single(await company.ListWorksAsync()).Lines);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("101")]
    public async Task Create_WastePercentOutside0To100_IsRejectedByTheApi(string waste)
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var paint = await company.CreateMaterialAsync("Dažai", "l");

        var response = await company.PostWorkAsync(WorkRequest("Sienų dažymas", "m²", (paint.Id, 0.150m, decimal.Parse(waste))));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await company.ListWorksAsync());
    }

    [Fact]
    public async Task Create_SameNameAndUnitInAnyLetterCase_IsRejectedAsDuplicate()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        await company.CreateWorkAsync(WorkRequest("Sienų dažymas", "m²"));

        var response = await company.PostWorkAsync(WorkRequest("sienų dažymas ", "m²"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Single(await company.ListWorksAsync());
    }

    [Fact]
    public async Task Create_SameNameWithDifferentUnit_IsAccepted()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        await company.CreateWorkAsync(WorkRequest("Sienų dažymas", "m²"));

        var response = await company.PostWorkAsync(WorkRequest("Sienų dažymas", "val."));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Update_ToAnotherWorksNameAndUnit_IsRejectedAsDuplicate()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        await company.CreateWorkAsync(WorkRequest("Sienų dažymas", "m²"));
        var other = await company.CreateWorkAsync(WorkRequest("Lubų dažymas", "m²"));

        var response = await company.Client.PutAsJsonAsync($"/api/works/{other.Id}", WorkRequest("SIENŲ DAŽYMAS", "m²"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReplacesTheWholeComposition()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var paint = await company.CreateMaterialAsync("Dažai", "l");
        var primer = await company.CreateMaterialAsync("Gruntas", "l");
        var work = await company.CreateWorkAsync(WorkRequest("Sienų dažymas", "m²", (paint.Id, 0.150m, 5), (primer.Id, 0.100m, 0)));

        var response = await company.Client.PutAsJsonAsync($"/api/works/{work.Id}",
            WorkRequest("Sienų dažymas", "m²", (paint.Id, 0.200m, 10)) with { LabourNorm = 0.300m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = (await company.Client.GetFromJsonAsync<WorkResponse>($"/api/works/{work.Id}"))!;
        Assert.Equal(0.300m, saved.LabourNorm);
        var line = Assert.Single(saved.Lines);
        Assert.Equal((paint.Id, 0.200m, 10m), (line.MaterialId, line.ConsumptionNorm, line.WastePercent));
    }

    [Fact]
    public async Task Create_WithUnknownMaterial_IsRejectedAndSavesNothing()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var response = await company.PostWorkAsync(WorkRequest("Sienų dažymas", "m²", (999, 0.150m, 5)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await company.ListWorksAsync());
    }

    [Fact]
    public async Task Create_WithAnotherCompanysMaterial_IsRejectedAndSavesNothing()
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        var betaPaint = await beta.CreateMaterialAsync("Dažai", "l");

        var response = await alpha.PostWorkAsync(WorkRequest("Sienų dažymas", "m²", (betaPaint.Id, 0.150m, 5)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await alpha.ListWorksAsync());
    }

    [Fact]
    public async Task Delete_RemovesTheWork()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var paint = await company.CreateMaterialAsync("Dažai", "l");
        var work = await company.CreateWorkAsync(WorkRequest("Sienų dažymas", "m²", (paint.Id, 0.150m, 5)));

        var response = await company.Client.DeleteAsync($"/api/works/{work.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await company.ListWorksAsync());
    }

    [Fact]
    public async Task MissingWork_Returns404()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var responses = new[]
        {
            await company.Client.GetAsync("/api/works/999"),
            await company.Client.PutAsJsonAsync("/api/works/999", WorkRequest("Sienų dažymas")),
            await company.Client.DeleteAsync("/api/works/999")
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
    }
}
