using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// The one place where estimates are calculated (BR-015–BR-017). It only reads the estimate's stored inputs,
/// rounds money through <see cref="MoneyRounding"/> and never rounds quantities or norms.
/// </summary>
public static class EstimateCalculator
{
    /// <summary>
    /// Material quantity for a work quantity: quantity × consumption norm × (1 + waste % / 100), not rounded.
    /// </summary>
    public static decimal MaterialQuantity(decimal workQuantity, decimal consumptionNorm, decimal wastePercent) =>
        workQuantity * consumptionNorm * (1 + wastePercent / 100);

    /// <summary>
    /// Calculates every line amount and the totals: Labour, Materials and Machinery and their sum, the direct costs;
    /// then overhead, profit and VAT applied in turn, each rounded, and the total as the sum of the rounded parts.
    /// Lines keep the order they were added in.
    /// </summary>
    public static EstimateCalculation Calculate(Estimate estimate)
    {
        var workLines = estimate.WorkLines.OrderBy(l => l.Id).Select(CalculateWorkLine).ToList();

        var labour = workLines.Sum(l => l.LabourCost);
        var materials = workLines.Sum(l => l.Materials);
        var machinery = workLines.Sum(l => l.Machinery);
        var directCosts = labour + materials + machinery;
        var overhead = MoneyRounding.Round(directCosts * estimate.OverheadPercent / 100);
        var profit = MoneyRounding.Round((directCosts + overhead) * estimate.ProfitPercent / 100);
        var vat = MoneyRounding.Round((directCosts + overhead + profit) * estimate.VatPercent / 100);

        return new EstimateCalculation(
            workLines, labour, materials, machinery, directCosts, overhead, profit, vat, directCosts + overhead + profit + vat);
    }

    private static WorkLineCalculation CalculateWorkLine(EstimateWorkLine line)
    {
        var labourCost = MoneyRounding.Round(line.Quantity * line.LabourNorm * line.LabourRate);
        var materials = line.MaterialLines.OrderBy(m => m.Id).Select(material =>
        {
            var quantity = MaterialQuantity(line.Quantity, material.ConsumptionNorm, material.WastePercent);
            return new MaterialLineCalculation(material, quantity, MoneyRounding.Round(quantity * material.Price));
        }).ToList();

        // Sums of already-rounded costs, so the parts always add up to the amount exactly.
        var materialCosts = materials.Where(m => m.Line.Kind == MaterialKind.Material).Sum(m => m.Cost);
        var machineryCosts = materials.Where(m => m.Line.Kind == MaterialKind.Machinery).Sum(m => m.Cost);
        var amount = labourCost + materialCosts + machineryCosts;

        return new WorkLineCalculation(
            line, line.Quantity * line.LabourNorm, labourCost, materials, materialCosts, machineryCosts, amount,
            MoneyRounding.Round(amount / line.Quantity));
    }
}
