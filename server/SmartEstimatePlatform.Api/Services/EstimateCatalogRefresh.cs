using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Brings an estimate's copied prices and norms up to date with the catalog (FR-024, spec 015). Lines keep no
/// reference to the catalog (BR-010), so they are matched by name and unit, the catalog's identity (BR-006).
/// Only values change: quantities, percentages and the set of lines stay as they are.
/// </summary>
public static class EstimateCatalogRefresh
{
    /// <summary>
    /// Updates the estimate's lines in memory and reports every changed value and every line that has no match.
    /// A work line takes its work's labour rate and norm; a material line takes its material's price, and its
    /// norm and waste from its work's composition when the work and the material in it are found.
    /// </summary>
    public static CatalogRefreshResult Apply(Estimate estimate, IReadOnlyList<CatalogWork> works, IReadOnlyList<CatalogMaterial> materials)
    {
        var changes = new List<RefreshChange>();
        var notFound = new List<RefreshMiss>();

        foreach (var line in estimate.WorkLines)
        {
            var work = works.FirstOrDefault(w => Matches(w.Name, w.Unit, line.WorkName, line.WorkUnit));
            if (work is null)
            {
                notFound.Add(new RefreshMiss(line.WorkName, null));
            }
            else
            {
                Update(line.WorkName, null, RefreshedField.LabourRate, line.LabourRate, work.LabourRate, line.ChangeLabourRate);
                Update(line.WorkName, null, RefreshedField.LabourNorm, line.LabourNorm, work.LabourNorm, line.ChangeLabourNorm);
            }

            // A composition line serves one material line, so a material listed twice is matched in order.
            var unusedComponents = work?.Components.ToList() ?? [];
            foreach (var materialLine in line.MaterialLines)
            {
                var material = materials.FirstOrDefault(m =>
                    Matches(m.Name, m.Unit, materialLine.MaterialName, materialLine.MaterialUnit));
                if (material is null)
                {
                    notFound.Add(new RefreshMiss(line.WorkName, materialLine.MaterialName));
                    continue;
                }

                Update(line.WorkName, materialLine.MaterialName, RefreshedField.Price, materialLine.Price, material.Price, materialLine.ChangePrice);

                var component = unusedComponents.FirstOrDefault(c =>
                    Matches(c.MaterialName, c.MaterialUnit, materialLine.MaterialName, materialLine.MaterialUnit));
                if (component is null)
                {
                    continue;
                }

                unusedComponents.Remove(component);
                var (norm, waste) = (materialLine.ConsumptionNorm, materialLine.WastePercent);
                Record(line.WorkName, materialLine.MaterialName, RefreshedField.ConsumptionNorm, norm, component.ConsumptionNorm);
                Record(line.WorkName, materialLine.MaterialName, RefreshedField.WastePercent, waste, component.WastePercent);
                materialLine.ChangeNorms(component.ConsumptionNorm, component.WastePercent);
            }
        }

        return new CatalogRefreshResult(changes, notFound);

        void Update(string workName, string? materialName, RefreshedField field, decimal oldValue, decimal newValue, Action<decimal> change)
        {
            if (Record(workName, materialName, field, oldValue, newValue))
            {
                change(newValue);
            }
        }

        bool Record(string workName, string? materialName, RefreshedField field, decimal oldValue, decimal newValue)
        {
            if (oldValue == newValue)
            {
                return false;
            }

            changes.Add(new RefreshChange(workName, materialName, field, oldValue, newValue));
            return true;
        }
    }

    // Names follow the catalog's case-insensitive uniqueness; units come from the fixed list and match exactly.
    private static bool Matches(string catalogName, string catalogUnit, string lineName, string lineUnit) =>
        string.Equals(catalogName, lineName, StringComparison.OrdinalIgnoreCase) && catalogUnit == lineUnit;
}
