using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services.Mapping;

namespace SmartEstimatePlatform.Api.Services;

/// <inheritdoc cref="IEstimateService" />
public class EstimateService(AppDbContext db) : IEstimateService
{
    /// <inheritdoc />
    public async Task<List<EstimateSummaryResponse>> ListForProjectAsync(int projectId)
    {
        await EnsureProjectExistsAsync(projectId);
        var estimates = await EstimatesWithLines()
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.Name).ThenBy(e => e.Id)
            .ToListAsync();
        return estimates.Select(e => e.ToSummary(EstimateCalculator.Calculate(e))).ToList();
    }

    /// <inheritdoc />
    public async Task<List<EstimateNameResponse>> ListNamesForProjectAsync(int projectId)
    {
        await EnsureProjectExistsAsync(projectId);
        var estimates = await db.Estimates
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.Name).ThenBy(e => e.Id)
            .ToListAsync();
        return estimates.Select(e => e.ToNameResponse()).ToList();
    }

    /// <inheritdoc />
    public async Task<EstimateNamesResponse> GetNamesAsync(int id) =>
        (await FindAsync(id)).ToNamesResponse();

    /// <inheritdoc />
    public async Task<EstimateResponse> CreateAsync(int projectId, CreateEstimateRequest request)
    {
        await EnsureProjectExistsAsync(projectId);
        var estimate = new Estimate(projectId, request.Name.Trim(), 0m, 0m, EstimateRules.DefaultVatPercent);
        db.Estimates.Add(estimate);
        await db.SaveChangesAsync();
        return Respond(estimate);
    }

    /// <inheritdoc />
    public async Task<EstimateResponse> GetAsync(int id) =>
        Respond(await FindAsync(id));

    /// <inheritdoc />
    public async Task<EstimateResponse> AddWorkLineAsync(int id, AddWorkLineRequest request)
    {
        var estimate = await FindDraftAsync(id);
        var work = await db.Works
                       .Include(w => w.Lines).ThenInclude(l => l.Material)
                       .SingleOrDefaultAsync(w => w.Id == request.WorkId)
                   ?? throw new UnknownWorkException(request.WorkId);

        // Copies, not references: later catalog changes never reach the estimate (BR-010).
        var materialLines = work.Lines.OrderBy(l => l.Id).Select(l => new EstimateMaterialLine(
            l.Material.Name, l.Material.Unit, l.Material.Price, l.ConsumptionNorm, l.WastePercent));
        estimate.AddWorkLine(new EstimateWorkLine(
            work.Name, work.Unit, work.LabourRate, work.LabourNorm, request.Quantity, materialLines));

        await db.SaveChangesAsync();
        return Respond(estimate);
    }

    /// <inheritdoc />
    public async Task<EstimateResponse> ChangeQuantityAsync(int id, int lineId, ChangeQuantityRequest request)
    {
        var estimate = await FindDraftAsync(id);
        FindLine(estimate, lineId).ChangeQuantity(request.Quantity);
        await db.SaveChangesAsync();
        return Respond(estimate);
    }

    /// <inheritdoc />
    public async Task<EstimateResponse> ChangePercentagesAsync(int id, ChangePercentagesRequest request)
    {
        var estimate = await FindDraftAsync(id);
        estimate.ChangePercentages(request.OverheadPercent, request.ProfitPercent, request.VatPercent);
        await db.SaveChangesAsync();
        return Respond(estimate);
    }

    /// <inheritdoc />
    public async Task<EstimateResponse> RemoveWorkLineAsync(int id, int lineId)
    {
        var estimate = await FindDraftAsync(id);
        estimate.RemoveWorkLine(FindLine(estimate, lineId));
        await db.SaveChangesAsync();
        return Respond(estimate);
    }

    /// <inheritdoc />
    public Task<EstimateResponse> ConfirmAsync(int id) =>
        ChangeStatusAsync(id, EstimateStatus.Draft, e => e.Confirm(), "The estimate is already confirmed.");

    /// <inheritdoc />
    public Task<EstimateResponse> ReturnToDraftAsync(int id) =>
        ChangeStatusAsync(id, EstimateStatus.Confirmed, e => e.ReturnToDraft(), "The estimate is already a draft.");

    // The one place where the status changes: only from the expected status, otherwise nothing changes.
    private async Task<EstimateResponse> ChangeStatusAsync(int id, EstimateStatus from, Action<Estimate> change, string rejection)
    {
        var estimate = await FindAsync(id);
        if (estimate.Status != from)
        {
            throw new EstimateStatusException(rejection);
        }

        change(estimate);
        await db.SaveChangesAsync();
        return Respond(estimate);
    }

    private static EstimateResponse Respond(Estimate estimate) =>
        estimate.ToResponse(EstimateCalculator.Calculate(estimate));

    private IQueryable<Estimate> EstimatesWithLines() =>
        db.Estimates.Include(e => e.WorkLines).ThenInclude(l => l.MaterialLines);

    private async Task<Estimate> FindAsync(int id) =>
        await EstimatesWithLines().SingleOrDefaultAsync(e => e.Id == id)
        ?? throw new NotFoundException("Estimate not found.");

    // Every change goes through here, so a confirmed estimate can never be changed (BR-013).
    private async Task<Estimate> FindDraftAsync(int id)
    {
        var estimate = await FindAsync(id);
        return estimate.Status == EstimateStatus.Draft ? estimate : throw new EstimateNotDraftException();
    }

    private static EstimateWorkLine FindLine(Estimate estimate, int lineId) =>
        estimate.WorkLines.SingleOrDefault(l => l.Id == lineId)
        ?? throw new NotFoundException("Line not found.");

    private async Task EnsureProjectExistsAsync(int projectId)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == projectId))
        {
            throw new NotFoundException("Project not found.");
        }
    }
}
