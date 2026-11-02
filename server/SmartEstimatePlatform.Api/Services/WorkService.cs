using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs.Works;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services.Mapping;

namespace SmartEstimatePlatform.Api.Services;

/// <inheritdoc cref="IWorkService" />
public class WorkService(AppDbContext db) : IWorkService
{
    /// <inheritdoc />
    public async Task<List<WorkResponse>> SearchAsync(string? search)
    {
        var query = WorksWithComposition();
        if (!string.IsNullOrWhiteSpace(search))
        {
            // LIKE follows the Name column's case-insensitive collation.
            var pattern = SearchPattern.Contains(search);
            query = query.Where(w => EF.Functions.Like(w.Name, pattern, SearchPattern.Escape));
        }

        var works = await query.OrderBy(w => w.Name).ThenBy(w => w.Unit).ToListAsync();
        return works.Select(w => w.ToResponse()).ToList();
    }

    /// <inheritdoc />
    public async Task<WorkResponse> GetAsync(int id) =>
        (await FindAsync(id)).ToResponse();

    /// <inheritdoc />
    public async Task<WorkResponse> CreateAsync(WorkRequest request)
    {
        var lines = await ToLinesAsync(request.Lines);
        var work = new Work(request.Name.Trim(), request.Unit, request.LabourRate, request.LabourNorm);
        work.ReplaceLines(lines);
        db.Works.Add(work);
        await SaveAsync(work);
        return work.ToResponse();
    }

    /// <inheritdoc />
    public async Task<WorkResponse> UpdateAsync(int id, WorkRequest request)
    {
        var work = await FindAsync(id);
        var lines = await ToLinesAsync(request.Lines);
        work.Update(request.Name.Trim(), request.Unit, request.LabourRate, request.LabourNorm);
        work.ReplaceLines(lines);
        await SaveAsync(work);
        return work.ToResponse();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var work = await FindAsync(id);
        db.Works.Remove(work);
        await db.SaveChangesAsync();
    }

    private IQueryable<Work> WorksWithComposition() =>
        db.Works.Include(w => w.Lines).ThenInclude(l => l.Material);

    private async Task<Work> FindAsync(int id) =>
        await WorksWithComposition().SingleOrDefaultAsync(w => w.Id == id)
        ?? throw new NotFoundException("Work not found.");

    // Every line must use a material of the current company (the company scoping hides any other).
    // Loading the materials also fills in each new line's Material for the response.
    private async Task<List<WorkMaterialLine>> ToLinesAsync(IReadOnlyCollection<WorkMaterialLineRequest> requests)
    {
        var materialIds = requests.Select(l => l.MaterialId).Distinct().ToList();
        var materials = await db.Materials.Where(m => materialIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id);
        var missing = materialIds.Where(id => !materials.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            throw new UnknownMaterialException(missing[0]);
        }

        return requests.Select(l => new WorkMaterialLine(l.MaterialId, l.ConsumptionNorm, l.WastePercent)).ToList();
    }

    private Task SaveAsync(Work work) =>
        UniqueSave.SaveAsync(
            db,
            () => DuplicateExistsAsync(work),
            () => new DuplicateWorkException(work.Name, work.Unit));

    // The Name column's collation makes this comparison case-insensitive.
    private Task<bool> DuplicateExistsAsync(Work work) =>
        db.Works.AnyAsync(w => w.Id != work.Id && w.Name == work.Name && w.Unit == work.Unit);
}
