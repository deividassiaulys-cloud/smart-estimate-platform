using System.Net;
using System.Net.Http.Json;
using ClosedXML.Excel;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Models.DTOs.Materials;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.EstimateLineEditingTests;

/// <summary>
/// Editing an estimate's own copies of prices and materials (FR-023, spec 014), on the worked example:
/// "Siena A++" 35 m² at 15 €/h × 0.8 h with Gipso plokštė, Varžtai and Plėvelė, 10 % / 15 % / 21 % (total 1263.26).
/// Every expected total is calculated by hand with BR-015–BR-017.
/// </summary>
public sealed class EstimateLineEditingTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    // ---- US1: prices ----

    [Fact]
    public async Task ChangeLabourRate_RecalculatesTheEstimate()
    {
        var (company, estimate) = await ArrangeAsync();
        var line = estimate.WorkLines[0];

        var changed = await Ok(await company.Client.PatchAsJsonAsync(LabourRatePath(estimate.Id, line.Id), new { labourRate = 18m }));

        Assert.Equal((18m, 504.00m), (changed.WorkLines[0].LabourRate, changed.WorkLines[0].LabourCost));
        AssertTotals(changed, 909.31m, 90.93m, 150.04m, 241.56m, 1391.84m);
    }

    [Fact]
    public async Task ChangeMaterialPrice_RecalculatesTheEstimate()
    {
        var (company, estimate) = await ArrangeAsync();
        var line = estimate.WorkLines[0];

        var changed = await Ok(await company.Client.PatchAsJsonAsync(PricePath(estimate.Id, line.Id, line.MaterialLines[0].Id), new { price = 4m }));

        var board = changed.WorkLines[0].MaterialLines[0];
        Assert.Equal((4m, 77m, 308.00m), (board.Price, board.Quantity, board.Cost));
        AssertTotals(changed, 786.81m, 78.68m, 129.82m, 209.02m, 1204.33m);
    }

    [Fact]
    public async Task ChangedPrices_LeaveTheCatalogAndOtherEstimatesUnchanged()
    {
        var (company, estimate) = await ArrangeAsync();
        var other = await company.CreateEstimateAsync("Kita sąmata");
        await company.Client.AddWorkLineAsync(other.Id, _workId, 35m);
        var line = estimate.WorkLines[0];

        await Ok(await company.Client.PatchAsJsonAsync(LabourRatePath(estimate.Id, line.Id), new { labourRate = 18m }));
        await Ok(await company.Client.PatchAsJsonAsync(PricePath(estimate.Id, line.Id, line.MaterialLines[0].Id), new { price = 4m }));

        var materials = (await company.Client.GetFromJsonAsync<List<MaterialResponse>>("/api/materials"))!;
        Assert.Equal(4.50m, materials.Single(m => m.Name == "Gipso plokštė").Price);
        var work = await company.Client.GetFromJsonAsync<Api.Models.DTOs.Works.WorkResponse>($"/api/works/{_workId}");
        Assert.Equal(15m, work!.LabourRate);
        var otherLine = (await company.Client.GetEstimateAsync(other.Id)).WorkLines[0];
        Assert.Equal((15m, 4.50m), (otherLine.LabourRate, otherLine.MaterialLines[0].Price));
    }

    [Fact]
    public async Task SamePriceAgain_IsAcceptedAndChangesNothing()
    {
        var (company, estimate) = await ArrangeAsync();
        var line = estimate.WorkLines[0];

        var changed = await Ok(await company.Client.PatchAsJsonAsync(PricePath(estimate.Id, line.Id, line.MaterialLines[0].Id), new { price = 4.50m }));

        AssertTotals(changed, 825.31m, 82.53m, 136.18m, 219.24m, 1263.26m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4.001)]
    [InlineData(99999999999999999)]
    public async Task InvalidPrices_AreRefusedAndNothingChanges(decimal value)
    {
        var (company, estimate) = await ArrangeAsync();
        var line = estimate.WorkLines[0];

        var rate = await company.Client.PatchAsJsonAsync(LabourRatePath(estimate.Id, line.Id), new { labourRate = value });
        var price = await company.Client.PatchAsJsonAsync(PricePath(estimate.Id, line.Id, line.MaterialLines[0].Id), new { price = value });

        Assert.Equal((HttpStatusCode.BadRequest, HttpStatusCode.BadRequest), (rate.StatusCode, price.StatusCode));
        AssertTotals(await company.Client.GetEstimateAsync(estimate.Id), 825.31m, 82.53m, 136.18m, 219.24m, 1263.26m);
    }

    // ---- US2: removal ----

    [Fact]
    public async Task RemoveMaterialLine_DropsItsCostAndKeepsTheOthers()
    {
        var (company, estimate) = await ArrangeAsync();
        var line = estimate.WorkLines[0];

        var changed = await Ok(await company.Client.DeleteAsync(MaterialPath(estimate.Id, line.Id, line.MaterialLines[2].Id)));

        Assert.Equal(["Gipso plokštė", "Varžtai"], changed.WorkLines[0].MaterialLines.Select(m => m.MaterialName));
        AssertTotals(changed, 784.88m, 78.49m, 129.51m, 208.50m, 1201.38m);
        AssertTotals(await company.Client.GetEstimateAsync(estimate.Id), 784.88m, 78.49m, 129.51m, 208.50m, 1201.38m);
    }

    [Fact]
    public async Task RemovingEveryMaterialLine_LeavesTheLabourCostOnly()
    {
        var (company, estimate) = await ArrangeAsync();
        var line = estimate.WorkLines[0];

        EstimateResponse changed = estimate;
        foreach (var material in line.MaterialLines)
        {
            changed = await Ok(await company.Client.DeleteAsync(MaterialPath(estimate.Id, line.Id, material.Id)));
        }

        Assert.Single(changed.WorkLines);
        Assert.Empty(changed.WorkLines[0].MaterialLines);
        AssertTotals(changed, 420.00m, 42.00m, 69.30m, 111.57m, 642.87m);
    }

    // ---- US3: replacement ----

    [Fact]
    public async Task ReplaceMaterial_CopiesTheNewMaterialAndKeepsNormAndWaste()
    {
        var (company, estimate) = await ArrangeAsync();
        var moistureBoard = await company.CreateMaterialAsync("Drėgmei atspari plokštė", "m²", 6.20m);
        var line = estimate.WorkLines[0];

        var changed = await Ok(await company.Client.PatchAsJsonAsync(
            ReplacePath(estimate.Id, line.Id, line.MaterialLines[0].Id), new { materialId = moistureBoard.Id }));

        var replaced = changed.WorkLines[0].MaterialLines[0];
        Assert.Equal(("Drėgmei atspari plokštė", "m²", 6.20m), (replaced.MaterialName, replaced.MaterialUnit, replaced.Price));
        Assert.Equal((2m, 10m, 77m, 477.40m), (replaced.ConsumptionNorm, replaced.WastePercent, replaced.Quantity, replaced.Cost));
        AssertTotals(changed, 956.21m, 95.62m, 157.77m, 254.02m, 1463.62m);
    }

    [Fact]
    public async Task ReplacedMaterial_IsACopy_LaterCatalogChangesDoNotReachIt()
    {
        var (company, estimate) = await ArrangeAsync();
        var moistureBoard = await company.CreateMaterialAsync("Drėgmei atspari plokštė", "m²", 6.20m);
        var line = estimate.WorkLines[0];
        await Ok(await company.Client.PatchAsJsonAsync(
            ReplacePath(estimate.Id, line.Id, line.MaterialLines[0].Id), new { materialId = moistureBoard.Id }));

        var update = await company.Client.PutAsJsonAsync($"/api/materials/{moistureBoard.Id}",
            new MaterialRequest { Name = "Drėgmei atspari plokštė", Unit = "m²", Price = 9.99m });
        update.EnsureSuccessStatusCode();

        Assert.Equal(6.20m, (await company.Client.GetEstimateAsync(estimate.Id)).WorkLines[0].MaterialLines[0].Price);
    }

    [Fact]
    public async Task ReplaceMaterial_WithAnotherUnitOrAnotherCompanysMaterial_IsRefused()
    {
        var (company, estimate) = await ArrangeAsync();
        var cement = await company.CreateMaterialAsync("Cementas", "kg", 0.25m);
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        var foreign = await beta.CreateMaterialAsync("Svetima plokštė", "m²", 1m);
        var line = estimate.WorkLines[0];
        var path = ReplacePath(estimate.Id, line.Id, line.MaterialLines[0].Id);

        var otherUnit = await company.Client.PatchAsJsonAsync(path, new { materialId = cement.Id });
        var otherCompany = await company.Client.PatchAsJsonAsync(path, new { materialId = foreign.Id });

        Assert.Equal((HttpStatusCode.BadRequest, HttpStatusCode.BadRequest), (otherUnit.StatusCode, otherCompany.StatusCode));
        Assert.Equal("Gipso plokštė", (await company.Client.GetEstimateAsync(estimate.Id)).WorkLines[0].MaterialLines[0].MaterialName);
    }

    // ---- Refusals shared by every change (FR-014-7) ----

    public static TheoryData<string> Changes => ["rate", "price", "replace", "remove"];

    [Theory]
    [MemberData(nameof(Changes))]
    public async Task ConfirmedEstimate_RefusesTheChange(string change)
    {
        var (company, estimate) = await ArrangeAsync();
        (await company.Client.ConfirmEstimateAsync(estimate.Id)).EnsureSuccessStatusCode();

        var response = await SendAsync(company.Client, change, estimate.Id, estimate.WorkLines[0].Id,
            estimate.WorkLines[0].MaterialLines[0].Id, await ReplacementIdAsync(company));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Changes))]
    public async Task Viewer_IsForbidden(string change)
    {
        var (company, estimate) = await ArrangeAsync();
        var viewer = await _factory.CreateViewerClientAsync(company);

        var response = await SendAsync(viewer, change, estimate.Id, estimate.WorkLines[0].Id,
            estimate.WorkLines[0].MaterialLines[0].Id, await ReplacementIdAsync(company));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Changes))]
    public async Task AnotherCompanysEstimate_IsNotFound(string change)
    {
        var (company, estimate) = await ArrangeAsync();
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        var betaMaterial = await beta.CreateMaterialAsync("Beta plokštė", "m²", 1m);

        var response = await SendAsync(beta.Client, change, estimate.Id, estimate.WorkLines[0].Id,
            estimate.WorkLines[0].MaterialLines[0].Id, betaMaterial.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Changes))]
    public async Task LinesOfAnotherEstimateOrWorkLine_AreNotFound(string change)
    {
        var (company, estimate) = await ArrangeAsync();
        var other = await company.Client.AddWorkLineAsync((await company.CreateEstimateAsync("Kita sąmata")).Id, _workId, 1m);
        var secondLine = (await company.Client.AddWorkLineAsync(estimate.Id, _workId, 1m)).WorkLines[1];
        var replacement = await ReplacementIdAsync(company);
        var line = estimate.WorkLines[0];

        // A work line of another estimate, and a material line of another work line of the same estimate.
        var foreignLine = await SendAsync(company.Client, change, estimate.Id, other.WorkLines[0].Id, other.WorkLines[0].MaterialLines[0].Id, replacement);
        var foreignMaterial = await SendAsync(company.Client, change, estimate.Id, line.Id, secondLine.MaterialLines[0].Id, replacement);

        Assert.Equal(HttpStatusCode.NotFound, foreignLine.StatusCode);
        if (change != "rate")
        {
            Assert.Equal(HttpStatusCode.NotFound, foreignMaterial.StatusCode);
        }
    }

    // ---- The Viewer view and the export show the edits (FR-014-8) ----

    [Fact]
    public async Task ViewerNamesAndExport_ReflectTheEdits()
    {
        var (company, estimate) = await ArrangeAsync();
        var moistureBoard = await company.CreateMaterialAsync("Drėgmei atspari plokštė", "m²", 6.20m);
        var line = estimate.WorkLines[0];
        await Ok(await company.Client.DeleteAsync(MaterialPath(estimate.Id, line.Id, line.MaterialLines[2].Id)));
        await Ok(await company.Client.PatchAsJsonAsync(ReplacePath(estimate.Id, line.Id, line.MaterialLines[0].Id), new { materialId = moistureBoard.Id }));
        await Ok(await company.Client.PatchAsJsonAsync(PricePath(estimate.Id, line.Id, line.MaterialLines[1].Id), new { price = 0.03m }));
        var viewer = await _factory.CreateViewerClientAsync(company);

        var names = (await viewer.GetFromJsonAsync<EstimateNamesResponse>($"/api/estimates/{estimate.Id}/names"))!;
        Assert.Equal(["Drėgmei atspari plokštė", "Varžtai"], names.Works[0].MaterialNames);

        var export = await company.Client.GetAsync($"/api/estimates/{estimate.Id}/export");
        export.EnsureSuccessStatusCode();
        using var file = new XLWorkbook(new MemoryStream(await export.Content.ReadAsByteArrayAsync()));
        var sheet = file.Worksheet("Sąmata");
        Assert.Equal(("Drėgmei atspari plokštė", 6.20m), (sheet.Cell("A8").GetString(), sheet.Cell("F8").GetValue<decimal>()));
        Assert.Equal(("Varžtai", 0.03m), (sheet.Cell("A9").GetString(), sheet.Cell("F9").GetValue<decimal>()));
        Assert.NotEqual("Plėvelė", sheet.Cell("A10").GetString());
    }

    // ---- Helpers ----

    private int _workId;

    private async Task<(LoggedInCompany Company, EstimateResponse Estimate)> ArrangeAsync()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        _workId = await company.CreateWallWorkAsync();
        var estimate = await company.CreateEstimateAsync();
        await company.Client.AddWorkLineAsync(estimate.Id, _workId, 35m);
        (await company.Client.PatchPercentagesAsync(estimate.Id, 10m, 15m, 21m)).EnsureSuccessStatusCode();
        return (company, await company.Client.GetEstimateAsync(estimate.Id));
    }

    private static async Task<int> ReplacementIdAsync(LoggedInCompany company) =>
        (await company.CreateMaterialAsync($"Plokštė {Guid.NewGuid():N}", "m²", 6.20m)).Id;

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string change, int id, int lineId, int materialLineId, int materialId) =>
        change switch
        {
            "rate" => client.PatchAsJsonAsync(LabourRatePath(id, lineId), new { labourRate = 18m }),
            "price" => client.PatchAsJsonAsync(PricePath(id, lineId, materialLineId), new { price = 4m }),
            "replace" => client.PatchAsJsonAsync(ReplacePath(id, lineId, materialLineId), new { materialId }),
            _ => client.DeleteAsync(MaterialPath(id, lineId, materialLineId))
        };

    private static string LabourRatePath(int id, int lineId) => $"/api/estimates/{id}/lines/{lineId}/labour-rate";

    private static string MaterialPath(int id, int lineId, int materialLineId) => $"/api/estimates/{id}/lines/{lineId}/materials/{materialLineId}";

    private static string PricePath(int id, int lineId, int materialLineId) => MaterialPath(id, lineId, materialLineId) + "/price";

    private static string ReplacePath(int id, int lineId, int materialLineId) => MaterialPath(id, lineId, materialLineId) + "/material";

    private static async Task<EstimateResponse> Ok(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EstimateResponse>())!;
    }

    private static void AssertTotals(EstimateResponse estimate, decimal direct, decimal overhead, decimal profit, decimal vat, decimal total) =>
        Assert.Equal((direct, overhead, profit, vat, total), (estimate.DirectCosts, estimate.Overhead, estimate.Profit, estimate.Vat, estimate.Total));
}
