using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using SmartEstimatePlatform.Api.Tests.TestSupport;
using static SmartEstimatePlatform.Api.Tests.TestSupport.CatalogRequests;

namespace SmartEstimatePlatform.Api.Tests.EstimateExportTests;

/// <summary>
/// Exporting an estimate to Excel (FR-027): the file is downloaded through the real API and read back, and must
/// match the estimate the editor shows, hold numbers without formulas, keep formula-like text as text (NFR-007),
/// and never be available to a Viewer (BR-005).
/// </summary>
public sealed class EstimateExportTests : IDisposable
{
    private const string MoneyFormat = "#,##0.00 \"€\"";

    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Export_MatchesTheEstimateOnScreen()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var workId = await company.CreateWallWorkAsync();
        var estimate = await company.CreateEstimateAsync();
        await company.Client.AddWorkLineAsync(estimate.Id, workId, 35m);
        await company.Client.PatchPercentagesAsync(estimate.Id, 10m, 15m, 21m);
        var onScreen = await company.Client.GetEstimateAsync(estimate.Id);

        using var file = await ExportAsync(company.Client, estimate.Id);
        var sheet = file.Worksheet("Sąmata");

        Assert.Equal(("Buto remontas", "Pradinė sąmata", "Juodraštis"),
            (sheet.Cell("B1").GetString(), sheet.Cell("B2").GetString(), sheet.Cell("B3").GetString()));
        Assert.Matches(new Regex(@"^\d{4}-\d{2}-\d{2}$"), sheet.Cell("B4").GetString());

        var work = onScreen.WorkLines[0];
        // Row 7: the work line; row 8: its labour row; rows 9-11: its material lines (FR-031).
        Assert.Equal((1m, "Siena A++", "m²"), (Number(sheet, "A7"), sheet.Cell("B7").GetString(), sheet.Cell("C7").GetString()));
        Assert.Equal((work.UnitPrice, work.Quantity, work.Amount, work.LabourCost, work.Materials, work.Machinery),
            (Number(sheet, "F7"), Number(sheet, "G7"), Number(sheet, "H7"), Number(sheet, "I7"), Number(sheet, "J7"), Number(sheet, "K7")));
        Assert.Equal(("Darbas", "val."), (sheet.Cell("B8").GetString(), sheet.Cell("C8").GetString()));
        Assert.Equal((work.LabourNorm, work.LabourRate, work.Hours, work.LabourCost, work.LabourCost),
            (Number(sheet, "D8"), Number(sheet, "F8"), Number(sheet, "G8"), Number(sheet, "H8"), Number(sheet, "I8")));
        for (var i = 0; i < work.MaterialLines.Count; i++)
        {
            var material = work.MaterialLines[i];
            var row = 9 + i;
            Assert.Equal(material.MaterialName, sheet.Cell(row, 2).GetString());
            Assert.Equal((material.ConsumptionNorm, material.WastePercent, material.Price, material.Quantity, material.Cost, material.Cost),
                (Number(sheet, $"D{row}"), Number(sheet, $"E{row}"), Number(sheet, $"F{row}"), Number(sheet, $"G{row}"),
                    Number(sheet, $"H{row}"), Number(sheet, $"J{row}")));
        }

        Assert.Equal(40.43m, Number(sheet, "H11"));
        Assert.Equal(onScreen.DirectCosts, Total(sheet, "Tiesioginės išlaidos"));
        Assert.Equal(onScreen.Overhead, Total(sheet, "Pridėtinės išlaidos (10 %)"));
        Assert.Equal(onScreen.Profit, Total(sheet, "Pelnas (15 %)"));
        Assert.Equal(onScreen.Vat, Total(sheet, "PVM (21 %)"));
        Assert.Equal(1263.26m, Total(sheet, "Iš viso su PVM"));
    }

    [Fact]
    public async Task Export_WritesNumberCellsWithFormatsAndNoFormulas()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var workId = await company.CreateWallWorkAsync();
        var estimate = await company.CreateEstimateAsync();
        await company.Client.AddWorkLineAsync(estimate.Id, workId, 35m);

        using var file = await ExportAsync(company.Client, estimate.Id);
        var sheet = file.Worksheet("Sąmata");

        Assert.Equal(XLDataType.Number, sheet.Cell("H7").DataType);
        Assert.Equal(MoneyFormat, sheet.Cell("H7").Style.NumberFormat.Format);
        Assert.Equal(MoneyFormat, sheet.Cell("F7").Style.NumberFormat.Format);
        Assert.Equal("#,##0.###", sheet.Cell("G8").Style.NumberFormat.Format);
        Assert.Equal("#,##0.###", sheet.Cell("G9").Style.NumberFormat.Format);
        Assert.Equal("0.## \"%\"", sheet.Cell("E9").Style.NumberFormat.Format);
        Assert.DoesNotContain(sheet.CellsUsed(), cell => cell.HasFormula);
    }

    [Fact]
    public async Task PrecisePricesAndNorms_AreWrittenInFullWithTheirOwnFormats()
    {
        // Feature 016: prices with 2 to 4 decimal places, norms with up to 6, amounts still with 2.
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var screws = await company.CreateMaterialAsync("Sraigtai terasoms", "vnt.", 0.0616m);
        var work = await company.CreateWorkAsync(CatalogRequests.WorkRequest("Terasa", "m²", (screws.Id, 0.000884m, 0))
            with { LabourRate = 25.1234m, LabourNorm = 0.042002m });
        var estimate = await company.CreateEstimateAsync();
        await company.Client.AddWorkLineAsync(estimate.Id, work.Id, 10m);

        using var file = await ExportAsync(company.Client, estimate.Id);
        var sheet = file.Worksheet("Sąmata");

        // Row 8 is the labour row (norm D, rate F), row 9 the material line (norm D, price F); amounts are in H.
        Assert.Equal((0.042002m, "#,##0.######"), (Number(sheet, "D8"), sheet.Cell("D8").Style.NumberFormat.Format));
        Assert.Equal((25.1234m, "#,##0.00## \"€\""), (Number(sheet, "F8"), sheet.Cell("F8").Style.NumberFormat.Format));
        Assert.Equal((0.000884m, "#,##0.######"), (Number(sheet, "D9"), sheet.Cell("D9").Style.NumberFormat.Format));
        Assert.Equal((0.0616m, "#,##0.00## \"€\""), (Number(sheet, "F9"), sheet.Cell("F9").Style.NumberFormat.Format));
        Assert.Equal(MoneyFormat, sheet.Cell("H8").Style.NumberFormat.Format);
        Assert.Equal(MoneyFormat, sheet.Cell("H9").Style.NumberFormat.Format);
    }

    [Fact]
    public async Task Export_OfAConfirmedEstimate_ShowsItsStatusAndFileName()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var estimate = await company.CreateEstimateAsync();
        await company.Client.ConfirmEstimateAsync(estimate.Id);

        var response = await company.Client.GetAsync($"/api/estimates/{estimate.Id}/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Pradinė sąmata.xlsx", response.Content.Headers.ContentDisposition?.FileNameStar);
        using var file = new XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
        Assert.Equal("Patvirtinta", file.Worksheet("Sąmata").Cell("B3").GetString());
    }

    [Fact]
    public async Task PercentageLabels_AreWrittenTheLithuanianWay()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var estimate = await company.CreateEstimateAsync();
        await company.Client.PatchPercentagesAsync(estimate.Id, 12.5m, 15m, 21m);

        using var file = await ExportAsync(company.Client, estimate.Id);
        var labels = file.Worksheet("Sąmata").CellsUsed(c => c.Address.ColumnNumber == 2).Select(c => c.GetString()).ToList();

        Assert.Contains("Pridėtinės išlaidos (12,5 %)", labels);
        Assert.Contains("Pelnas (15 %)", labels);
    }

    [Fact]
    public async Task FormulaLikeNames_AreExportedAsTextNotFormulas()
    {
        const string materialName = "=HYPERLINK(\"http://x\",\"y\")";
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var material = await company.CreateMaterialAsync(materialName, "kg", 1m);
        var work = await company.CreateWorkAsync(WorkRequest("-5", "m²", (material.Id, 1m, 0m)));
        var estimate = await company.CreateEstimateAsync();
        await company.Client.AddWorkLineAsync(estimate.Id, work.Id, 1m);

        using var file = await ExportAsync(company.Client, estimate.Id);
        var sheet = file.Worksheet("Sąmata");

        foreach (var (address, expected) in new[] { ("B7", "-5"), ("B9", materialName) })
        {
            var cell = sheet.Cell(address);
            Assert.Equal(XLDataType.Text, cell.DataType);
            Assert.False(cell.HasFormula);
            Assert.True(cell.Style.IncludeQuotePrefix);
            Assert.Equal(expected, cell.GetString());
        }
    }

    [Fact]
    public async Task Export_UsesTheElevenColumnLayoutWithKindsAndTheDirectCostsSplit()
    {
        // Spec 017 US4, on US2-2: the worked example's wall with "Įrankiai ir įrengimai" (Machinery, 0,20 €).
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var board = await company.CreateMaterialAsync("Gipso plokštė", "m²", 4.50m);
        var screws = await company.CreateMaterialAsync("Varžtai", "vnt.", 0.02m);
        var film = await company.CreateMaterialAsync("Plėvelė", "m²", 1.10m);
        var tools = await company.Client.PostAsJsonAsync("/api/materials", new Api.Models.DTOs.Materials.MaterialRequest
        {
            Name = "Įrankiai ir įrengimai", Unit = "vnt.", Price = 0.20m, Kind = Api.Models.Entities.MaterialKind.Machinery
        });
        var toolsId = (await tools.Content.ReadFromJsonAsync<Api.Models.DTOs.Materials.MaterialResponse>())!.Id;
        var work = await company.CreateWorkAsync(WorkRequest("Siena A++", "m²",
                (board.Id, 2m, 10m), (screws.Id, 25m, 5m), (film.Id, 1m, 5m), (toolsId, 1m, 0m))
            with { LabourRate = 15m, LabourNorm = 0.8m });
        var estimate = await company.CreateEstimateAsync();
        await company.Client.AddWorkLineAsync(estimate.Id, work.Id, 35m);
        await company.Client.PatchPercentagesAsync(estimate.Id, 10m, 15m, 21m);

        using var file = await ExportAsync(company.Client, estimate.Id);
        var sheet = file.Worksheet("Sąmata");

        Assert.Equal(["Nr.", "Pavadinimas", "Mato vnt.", "Norma", "Atliekos %", "Kaina", "Kiekis", "Suma", "Darbas", "Medžiagos", "Mechanizmai"],
            Enumerable.Range(1, 11).Select(column => sheet.Cell(6, column).GetString()));
        Assert.Equal((1m, 23.78m, 35m, 832.31m, 420m, 405.31m, 7m),
            (Number(sheet, "A7"), Number(sheet, "F7"), Number(sheet, "G7"), Number(sheet, "H7"), Number(sheet, "I7"), Number(sheet, "J7"), Number(sheet, "K7")));
        Assert.Equal((0.8m, 15m, 28m, 420m, 420m), (Number(sheet, "D8"), Number(sheet, "F8"), Number(sheet, "G8"), Number(sheet, "H8"), Number(sheet, "I8")));
        // Row 12 is the machinery line: its cost is under Mechanizmai, not Medžiagos.
        Assert.Equal(("Įrankiai ir įrengimai", 7m, 7m), (sheet.Cell("B12").GetString(), Number(sheet, "H12"), Number(sheet, "K12")));
        Assert.True(sheet.Cell("J12").IsEmpty());
        Assert.True(sheet.Cell("K9").IsEmpty());

        var direct = sheet.CellsUsed(c => c.Address.ColumnNumber == 2 && c.GetString() == "Tiesioginės išlaidos").Single().Address.RowNumber;
        Assert.Equal((832.31m, 420m, 405.31m, 7m), (Number(sheet, $"H{direct}"), Number(sheet, $"I{direct}"), Number(sheet, $"J{direct}"), Number(sheet, $"K{direct}")));
        Assert.Equal((420m, 405.31m, 7m), (Total(sheet, "Darbas"), Total(sheet, "Medžiagos"), Total(sheet, "Mechanizmai")));
        Assert.Equal((83.23m, 137.33m, 221.10m, 1273.97m),
            (Total(sheet, "Pridėtinės išlaidos (10 %)"), Total(sheet, "Pelnas (15 %)"), Total(sheet, "PVM (21 %)"), Total(sheet, "Iš viso su PVM")));
    }

    [Fact]
    public async Task Viewer_CannotExport()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var estimate = await company.CreateEstimateAsync();
        var viewer = await _factory.CreateViewerClientAsync(company);

        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync($"/api/estimates/{estimate.Id}/export")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync($"/api/estimates/{estimate.Id}/export")).StatusCode);
    }

    [Fact]
    public async Task MissingEstimate_Returns404()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        Assert.Equal(HttpStatusCode.NotFound, (await company.Client.GetAsync("/api/estimates/999/export")).StatusCode);
    }

    private static async Task<XLWorkbook> ExportAsync(HttpClient client, int estimateId)
    {
        var response = await client.GetAsync($"/api/estimates/{estimateId}/export");
        response.EnsureSuccessStatusCode();
        return new XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
    }

    private static decimal Number(IXLWorksheet sheet, string address)
    {
        var cell = sheet.Cell(address);
        Assert.Equal(XLDataType.Number, cell.DataType);
        return cell.GetValue<decimal>();
    }

    // The totals block is below the table, so a label that is also a row name ("Darbas") is taken from the bottom.
    private static decimal Total(IXLWorksheet sheet, string label)
    {
        var row = sheet.CellsUsed(c => c.Address.ColumnNumber == 2 && c.GetString() == label).Last().Address.RowNumber;
        return Number(sheet, $"H{row}");
    }
}
