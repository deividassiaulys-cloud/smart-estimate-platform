using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Manages the current company's material catalog. The company scoping from feature 011 means another
/// company's materials are never visible here and are reported as not found.
/// </summary>
public class MaterialService(AppDbContext db)
{
    /// <summary>
    /// The largest price that fits DECIMAL(18,2).
    /// </summary>
    public const decimal MaxPrice = 9_999_999_999_999_999.99m;

    /// <summary>
    /// Returns the materials whose name contains <paramref name="search"/>, ignoring letter case, sorted by name.
    /// An empty search returns every material.
    /// </summary>
    public Task<List<Material>> SearchAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = db.Materials.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            // LIKE follows the Name column's case-insensitive collation; % and _ typed by the user match literally.
            var pattern = "%" + search.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
            query = query.Where(m => EF.Functions.Like(m.Name, pattern, @"\"));
        }

        return query.OrderBy(m => m.Name).ThenBy(m => m.Unit).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Returns one material of the current company.
    /// </summary>
    /// <exception cref="NotFoundException">No material with this ID in the current company.</exception>
    public async Task<Material> GetAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Materials.SingleOrDefaultAsync(m => m.Id == id, cancellationToken)
        ?? throw new NotFoundException("Material not found.");

    /// <summary>
    /// Adds a material to the current company's catalog.
    /// </summary>
    /// <exception cref="InvalidMaterialException">The name, unit or price is invalid.</exception>
    /// <exception cref="DuplicateMaterialException">The same name and unit already exist.</exception>
    public async Task<Material> CreateAsync(MaterialRequest request, CancellationToken cancellationToken = default)
    {
        var material = new Material();
        Apply(material, request);
        db.Materials.Add(material);
        await SaveAsync(material, cancellationToken);
        return material;
    }

    /// <summary>
    /// Changes a material's name, unit and price.
    /// </summary>
    /// <exception cref="NotFoundException">No material with this ID in the current company.</exception>
    /// <exception cref="InvalidMaterialException">The name, unit or price is invalid.</exception>
    /// <exception cref="DuplicateMaterialException">Another material already has the same name and unit.</exception>
    public async Task<Material> UpdateAsync(int id, MaterialRequest request, CancellationToken cancellationToken = default)
    {
        var material = await GetAsync(id, cancellationToken);
        Apply(material, request);
        await SaveAsync(material, cancellationToken);
        return material;
    }

    /// <summary>
    /// Deletes a material. Estimates keep their own copies of material data, so they are not affected.
    /// </summary>
    /// <exception cref="NotFoundException">No material with this ID in the current company.</exception>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var material = await GetAsync(id, cancellationToken);
        db.Materials.Remove(material);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(Material material, MaterialRequest request)
    {
        var name = request.Name.Trim();
        if (name.Length == 0)
        {
            throw new InvalidMaterialException("Name is required.");
        }

        if (name.Length > Material.NameMaxLength)
        {
            throw new InvalidMaterialException($"Name must be at most {Material.NameMaxLength} characters.");
        }

        if (!MeasurementUnit.IsValid(request.Unit))
        {
            throw new InvalidMaterialException("Unit must be one of the fixed measurement units.");
        }

        if (request.Price < 0)
        {
            throw new InvalidMaterialException("Price cannot be negative.");
        }

        // Checked, not rounded: what the Manager typed is exactly what gets stored.
        if (decimal.Round(request.Price, 2) != request.Price)
        {
            throw new InvalidMaterialException("Price can have at most 2 decimal places.");
        }

        if (request.Price > MaxPrice)
        {
            throw new InvalidMaterialException("Price is too large.");
        }

        material.Name = name;
        material.Unit = request.Unit;
        material.Price = request.Price;
    }

    private async Task SaveAsync(Material material, CancellationToken cancellationToken)
    {
        if (await DuplicateExistsAsync(material, cancellationToken))
        {
            throw new DuplicateMaterialException(material.Name, material.Unit);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            // A concurrent request saved the same name and unit first; the unique index rejected this one.
            if (await DuplicateExistsAsync(material, cancellationToken))
            {
                throw new DuplicateMaterialException(material.Name, material.Unit);
            }

            throw;
        }
    }

    // The Name column's collation makes this comparison case-insensitive.
    private Task<bool> DuplicateExistsAsync(Material material, CancellationToken cancellationToken) =>
        db.Materials.AnyAsync(
            m => m.Id != material.Id && m.Name == material.Name && m.Unit == material.Unit,
            cancellationToken);
}
