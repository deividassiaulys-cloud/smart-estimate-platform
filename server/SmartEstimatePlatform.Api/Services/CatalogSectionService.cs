using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs.CatalogSections;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services.Mapping;

namespace SmartEstimatePlatform.Api.Services;

/// <inheritdoc cref="ICatalogSectionService" />
public class CatalogSectionService(AppDbContext db) : ICatalogSectionService
{
    /// <inheritdoc />
    public async Task<List<CatalogSectionResponse>> SearchAsync(string? search)
    {
        var query = SectionsWithWorks();
        if (!string.IsNullOrWhiteSpace(search))
        {
            // LIKE follows the Name column's case-insensitive collation.
            var pattern = SearchPattern.Contains(search);
            query = query.Where(s => EF.Functions.Like(s.Name, pattern, SearchPattern.Escape));
        }

        var sections = await query.OrderBy(s => s.Name).ThenBy(s => s.Unit).ToListAsync();
        return sections.Select(s => s.ToResponse()).ToList();
    }

    /// <inheritdoc />
    public async Task<CatalogSectionResponse> GetAsync(int id) =>
        (await FindAsync(id)).ToResponse();

    /// <inheritdoc />
    public async Task<CatalogSectionResponse> CreateAsync(CatalogSectionRequest request)
    {
        var section = new CatalogSection(request.Name.Trim(), request.Unit, await ToLinesAsync(request.Lines));
        db.CatalogSections.Add(section);
        await SaveAsync(section);
        return section.ToResponse();
    }

    /// <inheritdoc />
    public async Task<CatalogSectionResponse> UpdateAsync(int id, CatalogSectionRequest request)
    {
        var section = await FindAsync(id);
        var lines = await ToLinesAsync(request.Lines);
        section.Update(request.Name.Trim(), request.Unit);
        section.ReplaceLines(lines);
        await SaveAsync(section);
        return section.ToResponse();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var section = await FindAsync(id);
        db.CatalogSections.Remove(section);
        await db.SaveChangesAsync();
    }

    private IQueryable<CatalogSection> SectionsWithWorks() =>
        db.CatalogSections.Include(s => s.Lines.OrderBy(l => l.Id)).ThenInclude(l => l.Work);

    private async Task<CatalogSection> FindAsync(int id) =>
        await SectionsWithWorks().SingleOrDefaultAsync(s => s.Id == id)
        ?? throw new NotFoundException("Catalog section not found.");

    // Every line must use a work of the current company (the company scoping hides any other).
    // Loading the works also fills in each new line's Work for the response.
    private async Task<List<CatalogSectionLine>> ToLinesAsync(IReadOnlyCollection<CatalogSectionLineRequest> requests)
    {
        var workIds = requests.Select(l => l.WorkId).Distinct().ToList();
        var works = await db.Works.Where(w => workIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id);
        var missing = workIds.Where(id => !works.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            throw new UnknownWorkException(missing[0]);
        }

        return requests.Select(l => new CatalogSectionLine(l.WorkId, l.Coefficient)).ToList();
    }

    private Task SaveAsync(CatalogSection section) =>
        UniqueSave.SaveAsync(
            db,
            () => DuplicateExistsAsync(section),
            () => new DuplicateCatalogSectionException(section.Name, section.Unit));

    // The Name column's collation makes this comparison case-insensitive.
    private Task<bool> DuplicateExistsAsync(CatalogSection section) =>
        db.CatalogSections.AnyAsync(s => s.Id != section.Id && s.Name == section.Name && s.Unit == section.Unit);
}
