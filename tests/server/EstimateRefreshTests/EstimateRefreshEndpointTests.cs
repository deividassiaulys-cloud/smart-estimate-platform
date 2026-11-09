using System.Net;
using System.Net.Http.Json;
using ClosedXML.Excel;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Models.DTOs.Materials;
using SmartEstimatePlatform.Api.Models.DTOs.Works;
using SmartEstimatePlatform.Api.Services;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.EstimateRefreshTests;

/// <summary>
/// Updating an estimate from the catalog through the API (FR-024, spec 015), on the worked example (total 1263.26)
/// after the catalog changed "Siena A++" to 16 €/h and "Gipso plokštė" to 5.00 €. Expected totals are calculated
/// by hand with BR-015–BR-017.
/// </summary>
public sealed class EstimateRefreshEndpointTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    // ---- US1: preview ----

    [Fact]
    public async Task Preview_ListsTheChangesAndBothTotals()
    {
        var (company, estimate) = await ArrangeAsync();
        await ChangeCatalogAsync(company, labourRate: 16m, boardPrice: 5.00m);

        var preview = await PreviewAsync(company.Client, estimate.Id);

        Assert.Equal(
        [
            ("Siena A++", (string?)null, RefreshedField.LabourRate, 15m, 16m),
            ("Siena A++", "Gipso plokštė", RefreshedField.Price, 4.50m, 5.00m)
        ], preview.Changes.Select(c => (c.WorkName, c.MaterialName, c.Field, c.OldValue, c.NewValue)));
        Assert.Empty(preview.NotFound);
        Assert.Equal((1263.26m, 1365.05m), (preview.CurrentTotal, preview.NewTotal));
    }

    [Fact]
    public async Task Preview_SavesNothing()
    {
        var (company, estimate) = await ArrangeAsync();
        await ChangeCatalogAsync(company, labourRate: 16m, boardPrice: 5.00m);

        await PreviewAsync(company.Client, estimate.Id);

        var unchanged = await company.Client.GetEstimateAsync(estimate.Id);
        Assert.Equal((15m, 4.50m, 1263.26m), (unchanged.WorkLines[0].LabourRate, unchanged.WorkLines[0].MaterialLines[0].Price, unchanged.Total));
    }

    [Fact]
    public async Task Preview_ListsARenamedMaterialAsNotFound()
    {
        var (company, estimate) = await ArrangeAsync();
        var film = await MaterialAsync(company, "Plėvelė");
        (await company.Client.PutAsJsonAsync($"/api/materials/{film.Id}",
            new MaterialRequest { Name = "Garo izoliacinė plėvelė", Unit = "m²", Price = 1.10m })).EnsureSuccessStatusCode();

        var preview = await PreviewAsync(company.Client, estimate.Id);

        Assert.Equal([("Siena A++", (string?)"Plėvelė")], preview.NotFound.Select(m => (m.WorkName, m.MaterialName)));
    }

    [Fact]
    public async Task Preview_OfAnUpToDateEstimate_HasNoChanges()
    {
        var (company, estimate) = await ArrangeAsync();

        var preview = await PreviewAsync(company.Client, estimate.Id);

        Assert.Empty(preview.Changes);
        Assert.Equal(preview.CurrentTotal, preview.NewTotal);
    }

    // ---- US2: update ----

    [Fact]
    public async Task Refresh_AppliesTheCatalogAndMatchesThePreviewTotal()
    {
        var (company, estimate) = await ArrangeAsync();
        await ChangeCatalogAsync(company, labourRate: 16m, boardPrice: 5.00m);
        var preview = await PreviewAsync(company.Client, estimate.Id);

        var refreshed = await RefreshAsync(company.Client, estimate.Id);

        var line = refreshed.WorkLines[0];
        Assert.Equal((16m, 448.00m, 5.00m, 385.00m), (line.LabourRate, line.LabourCost, line.MaterialLines[0].Price, line.MaterialLines[0].Cost));
        Assert.Equal((891.81m, 89.18m, 147.15m, 236.91m, 1365.05m),
            (refreshed.DirectCosts, refreshed.Overhead, refreshed.Profit, refreshed.Vat, refreshed.Total));
        Assert.Equal(preview.NewTotal, refreshed.Total);
        Assert.Equal(1365.05m, (await company.Client.GetEstimateAsync(estimate.Id)).Total);
    }

    [Fact]
    public async Task Refresh_KeepsQuantitiesPercentagesAndLinesNotFound()
    {
        var (company, estimate) = await ArrangeAsync();
        await ChangeCatalogAsync(company, labourRate: 16m, boardPrice: 5.00m);
        var film = await MaterialAsync(company, "Plėvelė");
        (await company.Client.PutAsJsonAsync($"/api/materials/{film.Id}",
            new MaterialRequest { Name = "Garo izoliacinė plėvelė", Unit = "m²", Price = 9.99m })).EnsureSuccessStatusCode();

        var refreshed = await RefreshAsync(company.Client, estimate.Id);

        var line = refreshed.WorkLines[0];
        Assert.Equal((35m, 10m, 15m, 21m), (line.Quantity, refreshed.OverheadPercent, refreshed.ProfitPercent, refreshed.VatPercent));
        Assert.Equal(["Gipso plokštė", "Varžtai", "Plėvelė"], line.MaterialLines.Select(m => m.MaterialName));
        Assert.Equal(1.10m, line.MaterialLines[2].Price);
    }

    [Fact]
    public async Task Refresh_AppliesTheCompositionsNormAndWaste()
    {
        var (company, estimate) = await ArrangeAsync();
        await ChangeCatalogAsync(company, labourRate: 15m, boardPrice: 4.50m, boardNorm: 2.1m, boardWaste: 12m);

        var refreshed = await RefreshAsync(company.Client, estimate.Id);

        var board = refreshed.WorkLines[0].MaterialLines[0];
        Assert.Equal((2.1m, 12m, 82.32m, 370.44m), (board.ConsumptionNorm, board.WastePercent, board.Quantity, board.Cost));
    }

    [Fact]
    public async Task Refresh_LeavesTheCatalogAndOtherEstimatesUnchanged()
    {
        var (company, estimate) = await ArrangeAsync();
        var other = await company.CreateEstimateAsync("Kita sąmata");
        await company.Client.AddWorkLineAsync(other.Id, _workId, 35m);
        // The other estimate keeps today's 15 €/h when only this one is refreshed.
        await ChangeCatalogAsync(company, labourRate: 16m, boardPrice: 5.00m);

        await RefreshAsync(company.Client, estimate.Id);

        Assert.Equal(15m, (await company.Client.GetEstimateAsync(other.Id)).WorkLines[0].LabourRate);
        Assert.Equal(5.00m, (await MaterialAsync(company, "Gipso plokštė")).Price);
        Assert.Equal(16m, (await company.Client.GetFromJsonAsync<WorkResponse>($"/api/works/{_workId}"))!.LabourRate);
    }

    [Fact]
    public async Task Refresh_UsesTheCatalogAsItIsWhenConfirmed()
    {
        var (company, estimate) = await ArrangeAsync();
        await ChangeCatalogAsync(company, labourRate: 16m, boardPrice: 5.00m);
        await PreviewAsync(company.Client, estimate.Id);
        await ChangeCatalogAsync(company, labourRate: 16m, boardPrice: 5.50m);

        var refreshed = await RefreshAsync(company.Client, estimate.Id);

        Assert.Equal(5.50m, refreshed.WorkLines[0].MaterialLines[0].Price);
    }

    [Fact]
    public async Task Refresh_OfAnUpToDateEstimate_ChangesNothing()
    {
        var (company, estimate) = await ArrangeAsync();

        var refreshed = await RefreshAsync(company.Client, estimate.Id);

        Assert.Equal(1263.26m, refreshed.Total);
    }

    [Fact]
    public async Task ViewerNamesAndExport_ShowTheRefreshedEstimate()
    {
        var (company, estimate) = await ArrangeAsync();
        await ChangeCatalogAsync(company, labourRate: 16m, boardPrice: 5.00m);
        await RefreshAsync(company.Client, estimate.Id);
        var viewer = await _factory.CreateViewerClientAsync(company);

        var names = (await viewer.GetFromJsonAsync<EstimateNamesResponse>($"/api/estimates/{estimate.Id}/names"))!;
        Assert.Equal(["Gipso plokštė", "Varžtai", "Plėvelė"], names.Works[0].MaterialNames);

        var export = await company.Client.GetAsync($"/api/estimates/{estimate.Id}/export");
        export.EnsureSuccessStatusCode();
        using var file = new XLWorkbook(new MemoryStream(await export.Content.ReadAsByteArrayAsync()));
        var sheet = file.Worksheet("Sąmata");
        Assert.Equal((16m, 5.00m), (sheet.Cell("F7").GetValue<decimal>(), sheet.Cell("F8").GetValue<decimal>()));
    }

    // ---- Refusals (FR-015-6) ----

    public static TheoryData<bool> PreviewOrRefresh => [true, false];

    [Theory]
    [MemberData(nameof(PreviewOrRefresh))]
    public async Task ConfirmedEstimate_IsRefused(bool preview)
    {
        var (company, estimate) = await ArrangeAsync();
        (await company.Client.ConfirmEstimateAsync(estimate.Id)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(company.Client, estimate.Id, preview)).StatusCode);
    }

    [Theory]
    [MemberData(nameof(PreviewOrRefresh))]
    public async Task Viewer_IsForbidden(bool preview)
    {
        var (company, estimate) = await ArrangeAsync();
        var viewer = await _factory.CreateViewerClientAsync(company);

        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(viewer, estimate.Id, preview)).StatusCode);
    }

    [Theory]
    [MemberData(nameof(PreviewOrRefresh))]
    public async Task AnotherCompanysEstimate_IsNotFound(bool preview)
    {
        var (_, estimate) = await ArrangeAsync();
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");

        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(beta.Client, estimate.Id, preview)).StatusCode);
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

    // Changes the catalog's wall work (labour rate, and the board's norm and waste in its composition) and the board's price.
    private async Task ChangeCatalogAsync(LoggedInCompany company, decimal labourRate, decimal boardPrice, decimal boardNorm = 2m, decimal boardWaste = 10m)
    {
        var board = await MaterialAsync(company, "Gipso plokštė");
        (await company.Client.PutAsJsonAsync($"/api/materials/{board.Id}",
            new MaterialRequest { Name = board.Name, Unit = board.Unit, Price = boardPrice })).EnsureSuccessStatusCode();

        var work = (await company.Client.GetFromJsonAsync<WorkResponse>($"/api/works/{_workId}"))!;
        var request = new WorkRequest
        {
            Name = work.Name,
            Unit = work.Unit,
            LabourRate = labourRate,
            LabourNorm = work.LabourNorm,
            Lines = work.Lines.Select(l => new WorkMaterialLineRequest
            {
                MaterialId = l.MaterialId,
                ConsumptionNorm = l.MaterialId == board.Id ? boardNorm : l.ConsumptionNorm,
                WastePercent = l.MaterialId == board.Id ? boardWaste : l.WastePercent
            }).ToList()
        };
        (await company.Client.PutAsJsonAsync($"/api/works/{_workId}", request)).EnsureSuccessStatusCode();
    }

    private static async Task<MaterialResponse> MaterialAsync(LoggedInCompany company, string name) =>
        (await company.Client.GetFromJsonAsync<List<MaterialResponse>>("/api/materials"))!.Single(m => m.Name == name);

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, int estimateId, bool preview) =>
        preview
            ? client.GetAsync($"/api/estimates/{estimateId}/refresh")
            : client.PostAsync($"/api/estimates/{estimateId}/refresh", null);

    private static async Task<EstimateRefreshPreviewResponse> PreviewAsync(HttpClient client, int estimateId)
    {
        var response = await SendAsync(client, estimateId, preview: true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EstimateRefreshPreviewResponse>())!;
    }

    private static async Task<EstimateResponse> RefreshAsync(HttpClient client, int estimateId)
    {
        var response = await SendAsync(client, estimateId, preview: false);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EstimateResponse>())!;
    }
}
