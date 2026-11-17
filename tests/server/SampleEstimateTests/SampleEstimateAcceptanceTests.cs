using System.Globalization;
using System.Text.Json;
using SmartEstimatePlatform.Api.Models.DTOs.Works;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.SampleEstimateTests;

/// <summary>
/// The acceptance test of feature 016 (spec "Acceptance test", FR-016-9): the 81-line sample estimate in
/// docs/reference/sample-estimates, built through the real API as catalog works with waste 0 and added to one
/// estimate with overhead 1,5 %, profit 6 % and VAT 21 %, totals 135 414,29 € ± 0,50 € (the source PDF).
/// The sample files are only read.
/// </summary>
public sealed class SampleEstimateAcceptanceTests : IDisposable
{
    private const decimal PdfTotal = 135_414.29m;

    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task SampleEstimate_BuiltFromCatalogWorks_MatchesThePdfTotalWithin50Cents()
    {
        var (resources, lines) = LoadSample();
        var company = await _factory.RegisterCompanyAsync("Pavyzdys UAB", "pavyzdys@example.com");

        // One catalog material per (resource code, price rounded to 4 decimals); a code with several prices
        // gets its price in the name, so name + unit stay unique (BR-006).
        var materialKeys = lines.SelectMany(l => l.Resources)
            .Where(r => resources[r.Code].Type != "labour")
            .Select(r => (r.Code, Price: Price(r, resources)))
            .Distinct()
            .ToList();
        var codesWithSeveralPrices = materialKeys.GroupBy(k => k.Code).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
        var materialIds = new Dictionary<(string Code, decimal Price), int>();
        foreach (var key in materialKeys)
        {
            var resource = resources[key.Code];
            var (unit, unitNote) = Unit(resource.Unit);
            var priceNote = codesWithSeveralPrices.Contains(key.Code)
                ? $" [{key.Price.ToString(CultureInfo.InvariantCulture)} €]"
                : string.Empty;
            var material = await company.CreateMaterialAsync($"{key.Code}{priceNote} {resource.Name}{unitNote}", unit, key.Price);
            materialIds[key] = material.Id;
        }

        var estimate = await company.CreateEstimateAsync();
        (await company.Client.PatchPercentagesAsync(estimate.Id, 1.5m, 6m, 21m)).EnsureSuccessStatusCode();
        var materialLineCount = 0;

        foreach (var line in lines.Where(l => l.Quantity > 0))
        {
            var labour = line.Resources.Where(r => resources[r.Code].Type == "labour").ToList();
            var rate = labour.Count == 0 ? 0m : Price(labour[0], resources);
            var labourCostPerUnit = labour.Sum(r => r.Norm * Price(r, resources));
            var labourNorm = rate == 0 ? 0m : Round(labourCostPerUnit / rate, 6);

            var materialLines = line.Resources
                .Where(r => resources[r.Code].Type != "labour")
                .Select(r => new WorkMaterialLineRequest
                {
                    MaterialId = materialIds[(r.Code, Price(r, resources))],
                    ConsumptionNorm = Round(r.Norm, 6),
                    WastePercent = 0
                })
                .ToList();
            // SC-016-2: no norm is too small for 6 decimal places, so none is dropped.
            Assert.All(materialLines, m => Assert.True(m.ConsumptionNorm > 0, $"Line {line.No}: a norm rounds to 0."));
            materialLineCount += materialLines.Count;

            var (unit, unitNote) = Unit(line.Unit);
            var work = await company.CreateWorkAsync(new WorkRequest
            {
                Name = Truncate($"{line.No}. {line.Name}{unitNote}"),
                Unit = unit,
                LabourRate = rate,
                LabourNorm = labourNorm,
                Lines = materialLines
            });
            await company.Client.AddWorkLineAsync(estimate.Id, work.Id, line.Quantity);
        }

        var result = await company.Client.GetEstimateAsync(estimate.Id);

        Assert.Equal(lines.Count(l => l.Quantity > 0), result.WorkLines.Count);
        Assert.Equal(materialLineCount, result.WorkLines.Sum(w => w.MaterialLines.Count));
        Assert.InRange(result.Total, PdfTotal - 0.50m, PdfTotal + 0.50m);
    }

    private static decimal Round(decimal value, int places) => decimal.Round(value, places, MidpointRounding.AwayFromZero);

    // A resource's price on this line (the line may override the catalog price), rounded to 4 decimal places.
    private static decimal Price(SampleResourceLine line, IReadOnlyDictionary<string, SampleResource> resources) =>
        Round(line.Price ?? resources[line.Code].Price, 4);

    // The sample's units mapped to the fixed list (BR-008); a unit outside it becomes "vnt." and is kept in the name.
    private static (string Unit, string NameNote) Unit(string sampleUnit)
    {
        var unit = sampleUnit switch
        {
            "m2" => "m²",
            "m3" => "m³",
            "vnt" => "vnt.",
            "ltr" => "l",
            _ => sampleUnit
        };
        return MeasurementUnit.IsValid(unit) ? (unit, string.Empty) : ("vnt.", $" ({sampleUnit})");
    }

    private static string Truncate(string name) => name.Length <= 200 ? name : name[..200];

    private static (Dictionary<string, SampleResource> Resources, List<SampleLine> Lines) LoadSample()
    {
        var folder = SampleFolder();
        using var resourcesJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "resources.json")));
        using var estimateJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "sample-estimate.json")));

        var resources = resourcesJson.RootElement.GetProperty("resources").EnumerateArray()
            .Select(r => new SampleResource(
                r.GetProperty("code").GetString()!,
                r.GetProperty("name").GetString()!,
                r.GetProperty("unit").GetString()!,
                r.GetProperty("type").GetString()!,
                r.GetProperty("price").GetDecimal()))
            .ToDictionary(r => r.Code);

        var lines = estimateJson.RootElement.GetProperty("sections").EnumerateArray()
            .SelectMany(s => s.GetProperty("lines").EnumerateArray())
            .Select(l => new SampleLine(
                l.GetProperty("no").GetInt32(),
                l.GetProperty("name").GetString()!,
                l.GetProperty("unit").GetString()!,
                l.GetProperty("quantity").GetDecimal(),
                l.GetProperty("resources").EnumerateArray()
                    .Select(r => new SampleResourceLine(
                        r.GetProperty("code").GetString()!,
                        r.GetProperty("norm").GetDecimal(),
                        r.TryGetProperty("price", out var price) ? price.GetDecimal() : null))
                    .ToList()))
            .ToList();

        return (resources, lines);
    }

    // The repository's sample folder, found by walking up from the test's output directory.
    private static string SampleFolder()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var folder = Path.Combine(directory.FullName, "docs", "reference", "sample-estimates");
            if (Directory.Exists(folder))
            {
                return folder;
            }
        }

        throw new DirectoryNotFoundException("docs/reference/sample-estimates was not found above the test output directory.");
    }

    private sealed record SampleResource(string Code, string Name, string Unit, string Type, decimal Price);

    private sealed record SampleResourceLine(string Code, decimal Norm, decimal? Price);

    private sealed record SampleLine(int No, string Name, string Unit, decimal Quantity, List<SampleResourceLine> Resources);
}
