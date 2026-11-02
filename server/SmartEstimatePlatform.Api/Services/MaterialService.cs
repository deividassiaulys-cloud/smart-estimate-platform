using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs.Materials;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services.Mapping;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Manages the current company's material catalog. The company scoping from feature 011 means another
/// company's materials are never visible here and are reported as not found.
/// </summary>
public class MaterialService(AppDbContext db) : IMaterialService
{
    /// <inheritdoc />
    public async Task<List<MaterialResponse>> SearchAsync(string? search)
    {
        var query = db.Materials.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            // LIKE follows the Name column's case-insensitive collation.
            var pattern = SearchPattern.Contains(search);
            query = query.Where(m => EF.Functions.Like(m.Name, pattern, SearchPattern.Escape));
        }

        var materials = await query.OrderBy(m => m.Name).ThenBy(m => m.Unit).ToListAsync();
        return materials.Select(m => m.ToResponse()).ToList();
    }

    /// <inheritdoc />
    public async Task<MaterialResponse> GetAsync(int id) =>
        (await FindAsync(id)).ToResponse();

    /// <inheritdoc />
    public async Task<MaterialResponse> CreateAsync(MaterialRequest request)
    {
        var material = new Material(request.Name.Trim(), request.Unit, request.Price);
        db.Materials.Add(material);
        await SaveAsync(material);
        return material.ToResponse();
    }

    /// <inheritdoc />
    public async Task<MaterialResponse> UpdateAsync(int id, MaterialRequest request)
    {
        var material = await FindAsync(id);
        material.Update(request.Name.Trim(), request.Unit, request.Price);
        await SaveAsync(material);
        return material.ToResponse();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var material = await FindAsync(id);

        // A work's composition must never refer to a missing material, so a material in use stays.
        var workNames = await db.Works
            .Where(w => w.Lines.Any(l => l.MaterialId == id))
            .Select(w => w.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync();
        if (workNames.Count > 0)
        {
            throw new MaterialInUseException(workNames);
        }

        db.Materials.Remove(material);
        await db.SaveChangesAsync();
    }

    private async Task<Material> FindAsync(int id) =>
        await db.Materials.SingleOrDefaultAsync(m => m.Id == id)
        ?? throw new NotFoundException("Material not found.");

    private Task SaveAsync(Material material) =>
        UniqueSave.SaveAsync(
            db,
            () => DuplicateExistsAsync(material),
            () => new DuplicateMaterialException(material.Name, material.Unit));

    // The Name column's collation makes this comparison case-insensitive.
    private Task<bool> DuplicateExistsAsync(Material material) =>
        db.Materials.AnyAsync(
            m => m.Id != material.Id && m.Name == material.Name && m.Unit == material.Unit);
}
