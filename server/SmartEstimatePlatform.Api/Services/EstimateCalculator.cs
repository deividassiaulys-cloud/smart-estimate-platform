using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// The one place where estimates are calculated (BR-015–BR-017). It only reads the estimate's stored inputs,
/// rounds money through <see cref="MoneyRounding"/> and never rounds quantities or norms, except a work quantity
/// computed from a section (<see cref="SectionWorkQuantity"/>).
/// </summary>
public static class EstimateCalculator
{
    /// <summary>
    /// Material quantity for a work quantity: quantity × consumption norm × (1 + waste % / 100), not rounded.
    /// </summary>
    public static decimal MaterialQuantity(decimal workQuantity, decimal consumptionNorm, decimal wastePercent) =>
        workQuantity * consumptionNorm * (1 + wastePercent / 100);

    /// <summary>
    /// A work quantity computed from a section: section quantity × coefficient, rounded to 3 decimal places with an
    /// exact half away from zero, because a work quantity has at most 3 decimal places (BR-017). It may be 0, which the
    /// caller refuses.
    /// </summary>
    public static decimal SectionWorkQuantity(decimal sectionQuantity, decimal coefficient) =>
        decimal.Round(sectionQuantity * coefficient, 3, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Calculates every line amount, each section's subtotal and the totals: direct costs, then overhead, profit and
    /// VAT applied in turn, each rounded, and the total as the sum of the rounded parts. Lines and sections keep the
    /// order they were added in.
    /// </summary>
    public static EstimateCalculation Calculate(Estimate estimate)
    {
        var workLines = estimate.WorkLines.OrderBy(l => l.Id).Select(CalculateWorkLine).ToList();

        var directCosts = workLines.Sum(Amount);
        var sections = estimate.Sections.OrderBy(s => s.Id)
            .Select(section => new SectionCalculation(section, workLines.Where(l => InSection(l.Line, section)).Sum(Amount)))
            .ToList();
        var unsectioned = workLines.Where(l => l.Line.Section is null && l.Line.SectionId is null).Sum(Amount);
        var overhead = MoneyRounding.Round(directCosts * estimate.OverheadPercent / 100);
        var profit = MoneyRounding.Round((directCosts + overhead) * estimate.ProfitPercent / 100);
        var vat = MoneyRounding.Round((directCosts + overhead + profit) * estimate.VatPercent / 100);

        return new EstimateCalculation(
            workLines, sections, unsectioned, directCosts, overhead, profit, vat, directCosts + overhead + profit + vat);
    }

    // A new line knows its section by reference, a loaded one at least by its key.
    private static bool InSection(EstimateWorkLine line, EstimateSection section) =>
        line.Section == section || (section.Id != 0 && line.SectionId == section.Id);

    // A work line's labour and material costs, all already rounded.
    private static decimal Amount(WorkLineCalculation line) => line.LabourCost + line.MaterialLines.Sum(m => m.Cost);

    private static WorkLineCalculation CalculateWorkLine(EstimateWorkLine line)
    {
        var labourCost = MoneyRounding.Round(line.Quantity * line.LabourNorm * line.LabourRate);
        var materials = line.MaterialLines.OrderBy(m => m.Id).Select(material =>
        {
            var quantity = MaterialQuantity(line.Quantity, material.ConsumptionNorm, material.WastePercent);
            return new MaterialLineCalculation(material, quantity, MoneyRounding.Round(quantity * material.Price));
        }).ToList();

        return new WorkLineCalculation(line, labourCost, materials);
    }
}
