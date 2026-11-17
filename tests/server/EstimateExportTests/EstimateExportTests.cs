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
        Assert.Equal(("Siena A++", "m²"), (sheet.Cell("A7").GetString(), sheet.Cell("B7").GetString()));
        Assert.Equal((work.Quantity, work.LabourNorm, work.LabourRate, work.LabourCost),
            (Number(sheet, "C7"), Number(sheet, "D7"), Number(sheet, "F7"), Number(sheet, "G7")));
        for (var i = 0; i < work.MaterialLines.Count; i++)
        {
            var material = work.MaterialLines[i];
            var row = 8 + i;
            Assert.Equal(material.MaterialName, sheet.Cell(row, 1).GetString());
            Assert.Equal((material.Quantity, material.ConsumptionNorm, material.WastePercent, material.Price, material.Cost),
                (Number(sheet, $"C{row}"), Number(sheet, $"D{row}"), Number(sheet, $"E{row}"), Number(sheet, $"F{row}"), Number(sheet, $"G{row}")));
        }

        Assert.Equal(40.43m, Number(sheet, "G10"));
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

        Assert.Equal(XLDataType.Number, sheet.Cell("G7").DataType);
        Assert.Equal(MoneyFormat, sheet.Cell("G7").Style.NumberFormat.Format);
        Assert.Equal("#,##0.###", sheet.Cell("C9").Style.NumberFormat.Format);
        Assert.Equal("0.## \"%\"", sheet.Cell("E8").Style.NumberFormat.Format);
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

        // Row 7 is the work line (norm D, rate F, labour cost G), row 8 its material line (norm D, price F, cost G).
        Assert.Equal((0.042002m, "#,##0.######"), (Number(sheet, "D7"), sheet.Cell("D7").Style.NumberFormat.Format));
        Assert.Equal((25.1234m, "#,##0.00## \"€\""), (Number(sheet, "F7"), sheet.Cell("F7").Style.NumberFormat.Format));
        Assert.Equal((0.000884m, "#,##0.######"), (Number(sheet, "D8"), sheet.Cell("D8").Style.NumberFormat.Format));
        Assert.Equal((0.0616m, "#,##0.00## \"€\""), (Number(sheet, "F8"), sheet.Cell("F8").Style.NumberFormat.Format));
        Assert.Equal(MoneyFormat, sheet.Cell("G7").Style.NumberFormat.Format);
        Assert.Equal(MoneyFormat, sheet.Cell("G8").Style.NumberFormat.Format);
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
        var labels = file.Worksheet("Sąmata").CellsUsed(c => c.Address.ColumnNumber == 1).Select(c => c.GetString()).ToList();

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

        foreach (var (address, expected) in new[] { ("A7", "-5"), ("A8", materialName) })
        {
            var cell = sheet.Cell(address);
            Assert.Equal(XLDataType.Text, cell.DataType);
            Assert.False(cell.HasFormula);
            Assert.True(cell.Style.IncludeQuotePrefix);
            Assert.Equal(expected, cell.GetString());
        }
    }

    [Fact]
    public async Task EstimateWithSections_IsExportedGroupedWithSubtotals()
    {
        // Feature 019 US3: "=A++ siena" (a formula-like name, still text) with the wall work, plus one work without a section.
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var workId = await company.CreateWallWorkAsync();
        var section = await company.Client.PostAsJsonAsync("/api/catalog-sections", new Api.Models.DTOs.CatalogSections.CatalogSectionRequest
        {
            Name = "=A++ siena", Unit = "m²", Lines = [new() { WorkId = workId, Coefficient = 1m }]
        });
        var sectionId = (await section.Content.ReadFromJsonAsync<Api.Models.DTOs.CatalogSections.CatalogSectionResponse>())!.Id;
        var estimate = await company.CreateEstimateAsync();
        (await company.Client.PostAsJsonAsync($"/api/estimates/{estimate.Id}/sections",
            new Api.Models.DTOs.Estimates.AddSectionRequest { CatalogSectionId = sectionId, Quantity = 35m })).EnsureSuccessStatusCode();
        await company.Client.AddWorkLineAsync(estimate.Id, workId, 10m);
        await company.Client.PatchPercentagesAsync(estimate.Id, 10m, 15m, 21m);
        var onScreen = await company.Client.GetEstimateAsync(estimate.Id);

        using var file = await ExportAsync(company.Client, estimate.Id);
        var sheet = file.Worksheet("Sąmata");

        // Row 7: the section heading; rows 8-11: its work and 3 materials; row 12: its subtotal; then "Be skyriaus".
        Assert.Equal("=A++ siena — 35 m²", sheet.Cell("A7").GetString());
        Assert.True(sheet.Cell("A7").Style.IncludeQuotePrefix);
        Assert.False(sheet.Cell("A7").HasFormula);
        Assert.Equal("Siena A++", sheet.Cell("A8").GetString());
        Assert.Equal(("Skyriaus suma", onScreen.Sections[0].Subtotal), (sheet.Cell("A12").GetString(), Number(sheet, "G12")));
        Assert.Equal("Be skyriaus", sheet.Cell("A13").GetString());
        Assert.Equal(("Skyriaus suma", onScreen.UnsectionedSubtotal), (sheet.Cell("A18").GetString(), Number(sheet, "G18")));
        Assert.Equal(onScreen.DirectCosts, Number(sheet, "G12") + Number(sheet, "G18"));
        Assert.Equal(onScreen.Total, Total(sheet, "Iš viso su PVM"));
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

    private static decimal Total(IXLWorksheet sheet, string label)
    {
        var row = sheet.CellsUsed(c => c.Address.ColumnNumber == 1 && c.GetString() == label).Single().Address.RowNumber;
        return Number(sheet, $"G{row}");
    }
}
