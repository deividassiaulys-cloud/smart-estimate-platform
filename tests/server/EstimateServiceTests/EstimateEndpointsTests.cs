using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Tests.TestSupport;
using static SmartEstimatePlatform.Api.Tests.TestSupport.CatalogRequests;

namespace SmartEstimatePlatform.Api.Tests.EstimateServiceTests;

/// <summary>
/// Creating estimates and editing them in the Estimate Editor, through the real API, with the spec's worked example.
/// </summary>
public sealed class EstimateEndpointsTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private Task<LoggedInCompany> RegisterAsync() => _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

    [Fact]
    public async Task Create_StartsAsEmptyDraftWithDefaultPercentages()
    {
        var company = await RegisterAsync();

        var estimate = await company.CreateEstimateAsync("  Pradinė sąmata  ");

        Assert.Equal(("Pradinė sąmata", EstimateStatus.Draft), (estimate.Name, estimate.Status));
        Assert.Equal((0m, 0m, 21m), (estimate.OverheadPercent, estimate.ProfitPercent, estimate.VatPercent));
        Assert.Empty(estimate.WorkLines);
        Assert.Equal(0m, estimate.Total);
    }

    [Fact]
    public async Task AddWork_GeneratesMaterialLinesAndTheWorkedExampleTotal()
    {
        var company = await RegisterAsync();
        var workId = await company.CreateWallWorkAsync();
        var estimate = await company.CreateEstimateAsync();
        await company.Client.PatchPercentagesAsync(estimate.Id, 10m, 15m, 21m);

        var result = await company.Client.AddWorkLineAsync(estimate.Id, workId, 35m);

        var line = Assert.Single(result.WorkLines);
        Assert.Equal(("Siena A++", "m²", 35m, 420.00m), (line.WorkName, line.WorkUnit, line.Quantity, line.LabourCost));
        Assert.Equal(
            [("Gipso plokštė", 77m, 346.50m), ("Varžtai", 918.75m, 18.38m), ("Plėvelė", 36.75m, 40.43m)],
            line.MaterialLines.Select(m => (m.MaterialName, m.Quantity, m.Cost)));
        Assert.Equal((825.31m, 82.53m, 136.18m, 219.24m, 1263.26m),
            (result.DirectCosts, result.Overhead, result.Profit, result.Vat, result.Total));
        Assert.Equal(1263.26m, (await company.Client.GetEstimateAsync(estimate.Id)).Total);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task AddWork_QuantityNotGreaterThan0_IsRejectedAndAddsNothing(string quantity)
    {
        // Covers the former T071: the request's validation reaches the API before the service runs.
        var company = await RegisterAsync();
        var workId = await company.CreateWallWorkAsync();
        var estimate = await company.CreateEstimateAsync();

        var response = await company.Client.PostWorkLineAsync(estimate.Id, workId, decimal.Parse(quantity));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty((await company.Client.GetEstimateAsync(estimate.Id)).WorkLines);
    }

    [Fact]
    public async Task AddWork_UnknownOrAnotherCompanysWork_IsRejected()
    {
        var alpha = await RegisterAsync();
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        var betaWork = await beta.CreateWorkAsync(WorkRequest("Sienų dažymas"));
        var estimate = await alpha.CreateEstimateAsync();

        var unknown = await alpha.Client.PostWorkLineAsync(estimate.Id, 999, 1m);
        var foreign = await alpha.Client.PostWorkLineAsync(estimate.Id, betaWork.Id, 1m);

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
        Assert.Empty((await alpha.Client.GetEstimateAsync(estimate.Id)).WorkLines);
    }

    [Fact]
    public async Task MissingEstimateOrProject_Returns404()
    {
        var company = await RegisterAsync();

        var responses = new[]
        {
            await company.Client.GetAsync("/api/estimates/999"),
            await company.Client.PostWorkLineAsync(999, 1, 1m),
            await company.Client.GetAsync("/api/projects/999/estimates"),
            await company.Client.PostAsJsonAsync("/api/projects/999/estimates", new CreateEstimateRequest { Name = "Sąmata" })
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
    }

    [Fact]
    public async Task ProjectEstimates_AreListedByNameWithTheirTotals()
    {
        var company = await RegisterAsync();
        var workId = await company.CreateWallWorkAsync();
        var project = await company.Client.CreateProjectAsync("Buto remontas");
        var second = await CreateInProjectAsync(company, project.Id, "Virtuvė");
        await CreateInProjectAsync(company, project.Id, "Pradinė sąmata");
        await company.Client.AddWorkLineAsync(second.Id, workId, 35m);

        var list = (await company.Client.GetFromJsonAsync<List<EstimateSummaryResponse>>($"/api/projects/{project.Id}/estimates"))!;

        // Default percentages 0 / 0 / 21: 825.31 + round(825.31 × 21 % = 173.3151) = 825.31 + 173.32.
        Assert.Equal([("Pradinė sąmata", 0m), ("Virtuvė", 998.63m)], list.Select(e => (e.Name, e.Total)));
        Assert.All(list, e => Assert.Equal(EstimateStatus.Draft, e.Status));
    }

    [Fact]
    public async Task ChangeQuantity_RecalculatesEveryAmount()
    {
        var company = await RegisterAsync();
        var workId = await company.CreateWallWorkAsync();
        var estimate = await company.CreateEstimateAsync();
        var lineId = (await company.Client.AddWorkLineAsync(estimate.Id, workId, 35m)).WorkLines[0].Id;

        var response = await company.Client.PatchAsJsonAsync($"/api/estimates/{estimate.Id}/lines/{lineId}",
            new ChangeQuantityRequest { Quantity = 40m });

        var result = (await response.Content.ReadFromJsonAsync<EstimateResponse>())!;
        var line = Assert.Single(result.WorkLines);
        Assert.Equal((40m, 480.00m), (line.Quantity, line.LabourCost));
        Assert.Equal([88m, 1050m, 42m], line.MaterialLines.Select(m => m.Quantity));
        Assert.Equal(480.00m + 396.00m + 21.00m + 46.20m, result.DirectCosts);
    }

    // ---- Feature 017: kinds, Labour / Materials / Machinery and unit price ----

    [Fact]
    public async Task AddWork_CopiesTheKindAndSplitsTheAmounts()
    {
        var (company, estimate, _) = await ArrangeWallWithToolsAsync();

        var result = await company.Client.GetEstimateAsync(estimate.Id);

        var line = Assert.Single(result.WorkLines);
        Assert.Equal([MaterialKind.Material, MaterialKind.Material, MaterialKind.Material, MaterialKind.Machinery],
            line.MaterialLines.Select(m => m.Kind));
        Assert.Equal((28m, 420.00m, 405.31m, 7.00m, 832.31m, 23.78m),
            (line.Hours, line.LabourCost, line.Materials, line.Machinery, line.Amount, line.UnitPrice));
        Assert.Equal((420.00m, 405.31m, 7.00m, 832.31m), (result.Labour, result.Materials, result.Machinery, result.DirectCosts));
        Assert.Equal(1273.97m, result.Total);
    }

    [Fact]
    public async Task ChangeQuantity_RecalculatesThePartsAndTheUnitPrice()
    {
        var (company, estimate, lineId) = await ArrangeWallWithToolsAsync();

        var response = await company.Client.PatchAsJsonAsync($"/api/estimates/{estimate.Id}/lines/{lineId}",
            new ChangeQuantityRequest { Quantity = 40m });

        var line = Assert.Single((await response.Content.ReadFromJsonAsync<EstimateResponse>())!.WorkLines);
        // 40 m²: labour 480,00; materials 396,00 + 21,00 + 46,20; machinery 40 × 0,20.
        Assert.Equal((32m, 480.00m, 463.20m, 8.00m, 951.20m, 23.78m),
            (line.Hours, line.LabourCost, line.Materials, line.Machinery, line.Amount, line.UnitPrice));
    }

    // The worked example's wall plus "Įrankiai ir įrengimai" (Machinery, norm 1, waste 0, 0,20 €), 35 m², 10 / 15 / 21 %.
    private async Task<(LoggedInCompany Company, EstimateResponse Estimate, int LineId)> ArrangeWallWithToolsAsync()
    {
        var company = await RegisterAsync();
        var board = await company.CreateMaterialAsync("Gipso plokštė", "m²", 4.50m);
        var screws = await company.CreateMaterialAsync("Varžtai", "vnt.", 0.02m);
        var film = await company.CreateMaterialAsync("Plėvelė", "m²", 1.10m);
        var toolsResponse = await company.Client.PostAsJsonAsync("/api/materials", new Api.Models.DTOs.Materials.MaterialRequest
        {
            Name = "Įrankiai ir įrengimai", Unit = "vnt.", Price = 0.20m, Kind = MaterialKind.Machinery
        });
        var tools = (await toolsResponse.Content.ReadFromJsonAsync<Api.Models.DTOs.Materials.MaterialResponse>())!;
        var work = await company.CreateWorkAsync(WorkRequest("Siena A++", "m²",
                (board.Id, 2m, 10m), (screws.Id, 25m, 5m), (film.Id, 1m, 5m), (tools.Id, 1m, 0m))
            with { LabourRate = 15m, LabourNorm = 0.8m });
        var estimate = await company.CreateEstimateAsync();
        var lineId = (await company.Client.AddWorkLineAsync(estimate.Id, work.Id, 35m)).WorkLines[0].Id;
        (await company.Client.PatchPercentagesAsync(estimate.Id, 10m, 15m, 21m)).EnsureSuccessStatusCode();
        return (company, estimate, lineId);
    }

    [Fact]
    public async Task ChangePercentages_RecalculatesTheTotal()
    {
        var company = await RegisterAsync();
        var workId = await company.CreateWallWorkAsync();
        var estimate = await company.CreateEstimateAsync();
        await company.Client.AddWorkLineAsync(estimate.Id, workId, 35m);

        var response = await company.Client.PatchPercentagesAsync(estimate.Id, 10m, 15m, 21m);

        var result = (await response.Content.ReadFromJsonAsync<EstimateResponse>())!;
        Assert.Equal((10m, 15m, 21m), (result.OverheadPercent, result.ProfitPercent, result.VatPercent));
        Assert.Equal(1263.26m, result.Total);
    }

    [Theory]
    [InlineData(-1, 15, 21)]
    [InlineData(101, 15, 21)]
    [InlineData(10, -1, 21)]
    [InlineData(10, 101, 21)]
    [InlineData(10, 15, -1)]
    [InlineData(10, 15, 101)]
    public async Task ChangePercentages_OutsideTheRange_IsRejectedAndChangesNothing(decimal overhead, decimal profit, decimal vat)
    {
        // Covers the former T073: one case below and one above the range for each percentage.
        var company = await RegisterAsync();
        var estimate = await company.CreateEstimateAsync();

        var response = await company.Client.PatchPercentagesAsync(estimate.Id, overhead, profit, vat);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var unchanged = await company.Client.GetEstimateAsync(estimate.Id);
        Assert.Equal((0m, 0m, 21m), (unchanged.OverheadPercent, unchanged.ProfitPercent, unchanged.VatPercent));
    }

    [Fact]
    public async Task RemoveWorkLine_RemovesItsMaterialLinesAndUpdatesTheTotals()
    {
        var company = await RegisterAsync();
        var workId = await company.CreateWallWorkAsync();
        var estimate = await company.CreateEstimateAsync();
        await company.Client.AddWorkLineAsync(estimate.Id, workId, 35m);
        var twoLines = await company.Client.AddWorkLineAsync(estimate.Id, workId, 10m);
        var (keep, remove) = (twoLines.WorkLines[0], twoLines.WorkLines[1]);

        var response = await company.Client.DeleteAsync($"/api/estimates/{estimate.Id}/lines/{remove.Id}");

        var result = (await response.Content.ReadFromJsonAsync<EstimateResponse>())!;
        Assert.Equal(keep.Id, Assert.Single(result.WorkLines).Id);
        Assert.Equal(825.31m, result.DirectCosts);
        var missing = await company.Client.DeleteAsync($"/api/estimates/{estimate.Id}/lines/{remove.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    private static async Task<EstimateResponse> CreateInProjectAsync(LoggedInCompany company, int projectId, string name)
    {
        var response = await company.Client.PostAsJsonAsync($"/api/projects/{projectId}/estimates", new CreateEstimateRequest { Name = name });
        return (await response.Content.ReadFromJsonAsync<EstimateResponse>())!;
    }
}
