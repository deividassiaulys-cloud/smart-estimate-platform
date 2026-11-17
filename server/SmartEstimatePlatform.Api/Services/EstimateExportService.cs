using System.Globalization;
using ClosedXML.Excel;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <inheritdoc cref="IEstimateExportService" />
/// <remarks>
/// The file is built from the very response the editor renders, so it matches the screen by construction.
/// Amounts are number cells holding the server's values with no formulas; every text cell is stored as text
/// with a quote prefix, so a value such as "=HYPERLINK(...)" is never evaluated (NFR-007).
/// </remarks>
public class EstimateExportService(
    IEstimateService estimateService,
    IProjectService projectService,
    TimeProvider timeProvider) : IEstimateExportService
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string MoneyFormat = "#,##0.00 \"€\"";

    // Unit prices with 2 to 4 decimal places and norms with up to 6, as on screen (NFR-006, BR-017).
    private const string PriceFormat = "#,##0.00## \"€\"";
    private const string NormFormat = "#,##0.######";
    private const string QuantityFormat = "#,##0.###";
    private const string PercentFormat = "0.## \"%\"";
    // The table's columns, as in the Estimate Editor (FR-031).
    private const int NoColumn = 1;
    private const int NameColumn = 2;
    private const int UnitColumn = 3;
    private const int NormColumn = 4;
    private const int WasteColumn = 5;
    private const int PriceColumn = 6;
    private const int QuantityColumn = 7;
    private const int AmountColumn = 8;
    private const int LabourColumn = 9;
    private const int MaterialsColumn = 10;
    private const int MachineryColumn = 11;
    private const int LastColumn = MachineryColumn;

    private static readonly TimeZoneInfo Lithuania = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vilnius");

    // Labels are text, so their numbers are written the Lithuanian way ("12,5 %", NFR-012) whatever the server's culture.
    private static readonly CultureInfo Lithuanian = CultureInfo.GetCultureInfo("lt-LT");

    // Characters that are not allowed in file names on common systems.
    private static readonly char[] InvalidFileNameChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

    /// <inheritdoc />
    public async Task<ExportFile> ExportAsync(int id)
    {
        var estimate = await estimateService.GetAsync(id);
        var project = await projectService.GetAsync(estimate.ProjectId);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sąmata");
        var row = WriteHeader(sheet, project.Name, estimate);
        row = WriteLines(sheet, row + 1, estimate);
        WriteTotals(sheet, row + 1, estimate);
        sheet.Columns(1, LastColumn).AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new ExportFile(FileName(estimate.Name), ContentType, stream.ToArray());
    }

    private int WriteHeader(IXLWorksheet sheet, string projectName, EstimateResponse estimate)
    {
        var exported = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), Lithuania).ToString("yyyy-MM-dd");
        (string Label, string Value)[] header =
        [
            ("Projektas", projectName),
            ("Sąmata", estimate.Name),
            ("Būsena", EstimateStatusNames.Of(estimate.Status)),
            ("Eksportuota", exported)
        ];

        for (var i = 0; i < header.Length; i++)
        {
            Text(sheet.Cell(i + 1, 1), header[i].Label).Style.Font.Bold = true;
            Text(sheet.Cell(i + 1, 2), header[i].Value);
        }

        return header.Length + 1;
    }

    // The table: each work line, then its labour row and its material lines, in the editor's 11 columns (FR-031).
    // Returns the last row used.
    private static int WriteLines(IXLWorksheet sheet, int row, EstimateResponse estimate)
    {
        string[] headings =
        [
            "Nr.", "Pavadinimas", "Mato vnt.", "Norma", "Atliekos %", "Kaina", "Kiekis", "Suma", "Darbas", "Medžiagos", "Mechanizmai"
        ];
        for (var column = 1; column <= headings.Length; column++)
        {
            Text(sheet.Cell(row, column), headings[column - 1]).Style.Font.Bold = true;
        }

        var number = 0;
        foreach (var work in estimate.WorkLines)
        {
            row++;
            Number(sheet.Cell(row, NoColumn), ++number, "0");
            Text(sheet.Cell(row, NameColumn), work.WorkName).Style.Font.Bold = true;
            Text(sheet.Cell(row, UnitColumn), work.WorkUnit);
            Number(sheet.Cell(row, PriceColumn), work.UnitPrice, MoneyFormat);
            Number(sheet.Cell(row, QuantityColumn), work.Quantity, QuantityFormat);
            Number(sheet.Cell(row, AmountColumn), work.Amount, MoneyFormat);
            Number(sheet.Cell(row, LabourColumn), work.LabourCost, MoneyFormat);
            Number(sheet.Cell(row, MaterialsColumn), work.Materials, MoneyFormat);
            Number(sheet.Cell(row, MachineryColumn), work.Machinery, MoneyFormat);

            row++;
            Text(sheet.Cell(row, NameColumn), "Darbas").Style.Alignment.Indent = 1;
            Text(sheet.Cell(row, UnitColumn), "val.");
            Number(sheet.Cell(row, NormColumn), work.LabourNorm, NormFormat);
            Number(sheet.Cell(row, PriceColumn), work.LabourRate, PriceFormat);
            Number(sheet.Cell(row, QuantityColumn), work.Hours, QuantityFormat);
            Number(sheet.Cell(row, AmountColumn), work.LabourCost, MoneyFormat);
            Number(sheet.Cell(row, LabourColumn), work.LabourCost, MoneyFormat);

            foreach (var material in work.MaterialLines)
            {
                row++;
                Text(sheet.Cell(row, NameColumn), material.MaterialName).Style.Alignment.Indent = 1;
                Text(sheet.Cell(row, UnitColumn), material.MaterialUnit);
                Number(sheet.Cell(row, NormColumn), material.ConsumptionNorm, NormFormat);
                Number(sheet.Cell(row, WasteColumn), material.WastePercent, PercentFormat);
                Number(sheet.Cell(row, PriceColumn), material.Price, PriceFormat);
                Number(sheet.Cell(row, QuantityColumn), material.Quantity, QuantityFormat);
                Number(sheet.Cell(row, AmountColumn), material.Cost, MoneyFormat);
                var kindColumn = material.Kind == MaterialKind.Machinery ? MachineryColumn : MaterialsColumn;
                Number(sheet.Cell(row, kindColumn), material.Cost, MoneyFormat);
            }
        }

        return row;
    }

    // The three parts, the direct costs (also split into the three columns), the percentages and the total (BR-016).
    private static void WriteTotals(IXLWorksheet sheet, int row, EstimateResponse estimate)
    {
        (string Label, decimal Amount, bool Split)[] totals =
        [
            ("Darbas", estimate.Labour, false),
            ("Medžiagos", estimate.Materials, false),
            ("Mechanizmai", estimate.Machinery, false),
            ("Tiesioginės išlaidos", estimate.DirectCosts, true),
            ($"Pridėtinės išlaidos ({Percent(estimate.OverheadPercent)})", estimate.Overhead, false),
            ($"Pelnas ({Percent(estimate.ProfitPercent)})", estimate.Profit, false),
            ($"PVM ({Percent(estimate.VatPercent)})", estimate.Vat, false),
            ("Iš viso su PVM", estimate.Total, false)
        ];

        foreach (var (label, amount, split) in totals)
        {
            row++;
            Text(sheet.Cell(row, NameColumn), label);
            Number(sheet.Cell(row, AmountColumn), amount, MoneyFormat);
            if (split)
            {
                Number(sheet.Cell(row, LabourColumn), estimate.Labour, MoneyFormat);
                Number(sheet.Cell(row, MaterialsColumn), estimate.Materials, MoneyFormat);
                Number(sheet.Cell(row, MachineryColumn), estimate.Machinery, MoneyFormat);
            }
        }

        sheet.Row(row).Style.Font.Bold = true;
    }

    private static string Percent(decimal value) => value.ToString("0.##", Lithuanian) + " %";

    // Every text cell goes through here: stored as text with a quote prefix, never as a formula (NFR-007).
    private static IXLCell Text(IXLCell cell, string value)
    {
        cell.SetValue(value);
        cell.Style.IncludeQuotePrefix = true;
        return cell;
    }

    // A number cell holding exactly the server's value, formatted like the screen; never a formula.
    private static void Number(IXLCell cell, decimal value, string format)
    {
        cell.SetValue(value);
        cell.Style.NumberFormat.Format = format;
    }

    private static string FileName(string estimateName)
    {
        var safe = new string(estimateName.Select(c => char.IsControl(c) || InvalidFileNameChars.Contains(c) ? '_' : c).ToArray()).Trim();
        return (safe.Length == 0 ? "samata" : safe) + ".xlsx";
    }
}
