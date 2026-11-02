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
            // LIKE follows the Name column's case-insensitive collation; % and _ typed by the user match literally.
            var pattern = "%" + search.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
            query = query.Where(m => EF.Functions.Like(m.Name, pattern, @"\"));
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
        db.Materials.Remove(material);
        await db.SaveChangesAsync();
    }

    private async Task<Material> FindAsync(int id) =>
        await db.Materials.SingleOrDefaultAsync(m => m.Id == id)
        ?? throw new NotFoundException("Material not found.");

    private async Task SaveAsync(Material material)
    {
        if (await DuplicateExistsAsync(material))
        {
            throw new DuplicateMaterialException(material.Name, material.Unit);
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            // A concurrent request saved the same name and unit first; the unique index rejected this one.
            if (await DuplicateExistsAsync(material))
            {
                throw new DuplicateMaterialException(material.Name, material.Unit);
            }

            throw;
        }
    }

    // The Name column's collation makes this comparison case-insensitive.
    private Task<bool> DuplicateExistsAsync(Material material) =>
        db.Materials.AnyAsync(
            m => m.Id != material.Id && m.Name == material.Name && m.Unit == material.Unit);
}
