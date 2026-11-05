using System.Globalization;
using ClosedXML.Excel;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;

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
    private const string QuantityFormat = "#,##0.###";
    private const string PercentFormat = "0.## \"%\"";
    private const int LastColumn = 7;

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

    // The table: each work line followed by its material lines, as in the editor. Returns the last row used.
    private static int WriteLines(IXLWorksheet sheet, int row, EstimateResponse estimate)
    {
        string[] headings = ["Pavadinimas", "Mato vnt.", "Kiekis", "Norma", "Atliekos %", "Kaina / Įkainis", "Suma"];
        for (var column = 1; column <= headings.Length; column++)
        {
            Text(sheet.Cell(row, column), headings[column - 1]).Style.Font.Bold = true;
        }

        foreach (var work in estimate.WorkLines)
        {
            row++;
            Text(sheet.Cell(row, 1), work.WorkName).Style.Font.Bold = true;
            Text(sheet.Cell(row, 2), work.WorkUnit);
            Number(sheet.Cell(row, 3), work.Quantity, QuantityFormat);
            Number(sheet.Cell(row, 4), work.LabourNorm, QuantityFormat);
            Number(sheet.Cell(row, 6), work.LabourRate, MoneyFormat);
            Number(sheet.Cell(row, 7), work.LabourCost, MoneyFormat);

            foreach (var material in work.MaterialLines)
            {
                row++;
                Text(sheet.Cell(row, 1), material.MaterialName).Style.Alignment.Indent = 1;
                Text(sheet.Cell(row, 2), material.MaterialUnit);
                Number(sheet.Cell(row, 3), material.Quantity, QuantityFormat);
                Number(sheet.Cell(row, 4), material.ConsumptionNorm, QuantityFormat);
                Number(sheet.Cell(row, 5), material.WastePercent, PercentFormat);
                Number(sheet.Cell(row, 6), material.Price, MoneyFormat);
                Number(sheet.Cell(row, 7), material.Cost, MoneyFormat);
            }
        }

        return row;
    }

    private static void WriteTotals(IXLWorksheet sheet, int row, EstimateResponse estimate)
    {
        (string Label, decimal Amount)[] totals =
        [
            ("Tiesioginės išlaidos", estimate.DirectCosts),
            ($"Pridėtinės išlaidos ({Percent(estimate.OverheadPercent)})", estimate.Overhead),
            ($"Pelnas ({Percent(estimate.ProfitPercent)})", estimate.Profit),
            ($"PVM ({Percent(estimate.VatPercent)})", estimate.Vat),
            ("Iš viso su PVM", estimate.Total)
        ];

        foreach (var (label, amount) in totals)
        {
            row++;
            Text(sheet.Cell(row, 1), label);
            Number(sheet.Cell(row, LastColumn), amount, MoneyFormat);
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
