namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// A calculation in a project. It stores only inputs (its work lines with copied prices and norms, and its
/// percentages); every amount is calculated when it is read, so totals can never disagree with the lines.
/// </summary>
public class Estimate : ICompanyScoped
{
    // Used by EF Core when loading an estimate.
    private Estimate()
    {
    }

    /// <summary>
    /// Creates a Draft estimate with no lines and the given starting percentages.
    /// </summary>
    public Estimate(int projectId, string name, decimal overheadPercent, decimal profitPercent, decimal vatPercent)
    {
        ProjectId = projectId;
        Name = name;
        Status = EstimateStatus.Draft;
        ChangePercentages(overheadPercent, profitPercent, vatPercent);
    }

    public int Id { get; private set; }

    public int ProjectId { get; private set; }

    public Project Project { get; private set; } = null!;

    /// <summary>
    /// Trimmed name, shown in the project's estimate list.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    public EstimateStatus Status { get; private set; }

    /// <summary>
    /// Overhead as a percentage of the direct costs.
    /// </summary>
    public decimal OverheadPercent { get; private set; }

    /// <summary>
    /// Profit as a percentage of the direct costs plus overhead.
    /// </summary>
    public decimal ProfitPercent { get; private set; }

    /// <summary>
    /// VAT as a percentage of the direct costs plus overhead plus profit.
    /// </summary>
    public decimal VatPercent { get; private set; }

    public int CompanyId { get; private set; }

    public Company Company { get; private set; } = null!;

    /// <summary>
    /// The works added to the estimate, each with its generated material lines.
    /// </summary>
    public List<EstimateWorkLine> WorkLines { get; private set; } = [];

    /// <summary>
    /// Replaces the three percentages with already validated values.
    /// </summary>
    public void ChangePercentages(decimal overheadPercent, decimal profitPercent, decimal vatPercent)
    {
        OverheadPercent = overheadPercent;
        ProfitPercent = profitPercent;
        VatPercent = vatPercent;
    }

    /// <summary>
    /// Adds a work line with its material lines.
    /// </summary>
    public void AddWorkLine(EstimateWorkLine workLine) => WorkLines.Add(workLine);

    /// <summary>
    /// Removes a work line; its material lines go with it.
    /// </summary>
    public void RemoveWorkLine(EstimateWorkLine workLine) => WorkLines.Remove(workLine);
}
