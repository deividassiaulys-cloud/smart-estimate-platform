using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Data;

/// <summary>
/// EF Core database context for the platform's companies, their users, their material and work catalogs, their projects and their estimates.
/// Every <see cref="ICompanyScoped"/> entity is filtered to the current company, so another company's
/// records are never visible. Only AuthService may bypass this, with IgnoreQueryFilters.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser? currentUser = null)
    : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Material> Materials => Set<Material>();

    public DbSet<Work> Works => Set<Work>();

    public DbSet<WorkMaterialLine> WorkMaterialLines => Set<WorkMaterialLine>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Estimate> Estimates => Set<Estimate>();

    public DbSet<EstimateWorkLine> EstimateWorkLines => Set<EstimateWorkLine>();

    public DbSet<EstimateMaterialLine> EstimateMaterialLines => Set<EstimateMaterialLine>();

    // Read by the query filters each time a query runs, so each request sees only its own company.
    private int? CurrentCompanyId => currentUser?.CompanyId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            // Accent-sensitive, case-insensitive collation so the unique index rejects
            // emails that differ only by letter case, including concurrent registrations.
            user.Property(u => u.Email)
                .HasMaxLength(256)
                .UseCollation("utf8mb4_0900_as_ci");

            user.HasIndex(u => u.Email).IsUnique();

            user.HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CompanyId);
        });

        modelBuilder.Entity<Material>(material =>
        {
            // Same case-insensitive collation as emails, so the unique index and search ignore letter case.
            material.Property(m => m.Name)
                .HasMaxLength(MaterialRules.NameMaxLength)
                .UseCollation("utf8mb4_0900_as_ci");

            material.Property(m => m.Unit).HasMaxLength(16);
            material.Property(m => m.Price).HasPrecision(18, 2);

            material.HasIndex(m => new { m.CompanyId, m.Name, m.Unit }).IsUnique();

            material.HasOne(m => m.Company)
                .WithMany()
                .HasForeignKey(m => m.CompanyId);
        });

        modelBuilder.Entity<Work>(work =>
        {
            // Same case-insensitive collation as materials, so the unique index and search ignore letter case.
            work.Property(w => w.Name)
                .HasMaxLength(WorkRules.NameMaxLength)
                .UseCollation("utf8mb4_0900_as_ci");

            work.Property(w => w.Unit).HasMaxLength(16);
            work.Property(w => w.LabourRate).HasPrecision(18, 2);
            work.Property(w => w.LabourNorm).HasPrecision(18, 3);

            work.HasIndex(w => new { w.CompanyId, w.Name, w.Unit }).IsUnique();

            work.HasOne(w => w.Company)
                .WithMany()
                .HasForeignKey(w => w.CompanyId);

            work.HasMany(w => w.Lines)
                .WithOne(l => l.Work)
                .HasForeignKey(l => l.WorkId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkMaterialLine>(line =>
        {
            line.Property(l => l.ConsumptionNorm).HasPrecision(18, 3);
            line.Property(l => l.WastePercent).HasPrecision(5, 2);

            // A line has no CompanyId of its own; it belongs to its work's company, so it is filtered like
            // its work and its material (both filtered by company) and is never seen without them.
            line.HasQueryFilter(l => l.Work.CompanyId == CurrentCompanyId);

            // A material used in a work cannot be deleted (MaterialService reports it first).
            // No unique index on (WorkId, MaterialId): the same material may repeat in one work (BR-009).
            line.HasOne(l => l.Material)
                .WithMany()
                .HasForeignKey(l => l.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Project>(project =>
        {
            // No unique index: two projects of a company may share a name.
            project.Property(p => p.Name).HasMaxLength(ProjectRules.NameMaxLength);
            project.Property(p => p.Description).HasMaxLength(ProjectRules.DescriptionMaxLength);

            project.HasOne(p => p.Company)
                .WithMany()
                .HasForeignKey(p => p.CompanyId);
        });

        modelBuilder.Entity<Estimate>(estimate =>
        {
            estimate.Property(e => e.Name).HasMaxLength(EstimateRules.NameMaxLength);
            estimate.Property(e => e.OverheadPercent).HasPrecision(5, 2);
            estimate.Property(e => e.ProfitPercent).HasPrecision(5, 2);
            estimate.Property(e => e.VatPercent).HasPrecision(5, 2);

            // Deleting a project deletes its estimates (BR-011).
            estimate.HasOne(e => e.Project)
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            estimate.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId);

            estimate.HasMany(e => e.WorkLines)
                .WithOne(l => l.Estimate)
                .HasForeignKey(l => l.EstimateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Estimate lines hold copies of the catalog data and have no foreign keys to it (BR-010).
        modelBuilder.Entity<EstimateWorkLine>(line =>
        {
            line.Property(l => l.WorkName).HasMaxLength(WorkRules.NameMaxLength);
            line.Property(l => l.WorkUnit).HasMaxLength(16);
            line.Property(l => l.LabourRate).HasPrecision(18, 2);
            line.Property(l => l.LabourNorm).HasPrecision(18, 3);
            line.Property(l => l.Quantity).HasPrecision(18, 3);

            // Removing a work line removes its material lines.
            line.HasMany(l => l.MaterialLines)
                .WithOne(m => m.WorkLine)
                .HasForeignKey(m => m.WorkLineId)
                .OnDelete(DeleteBehavior.Cascade);

            // Filtered like its estimate, which is filtered by company.
            line.HasQueryFilter(l => l.Estimate.CompanyId == CurrentCompanyId);
        });

        modelBuilder.Entity<EstimateMaterialLine>(line =>
        {
            line.Property(m => m.MaterialName).HasMaxLength(MaterialRules.NameMaxLength);
            line.Property(m => m.MaterialUnit).HasMaxLength(16);
            line.Property(m => m.Price).HasPrecision(18, 2);
            line.Property(m => m.ConsumptionNorm).HasPrecision(18, 3);
            line.Property(m => m.WastePercent).HasPrecision(5, 2);

            // Filtered like its work line's estimate.
            line.HasQueryFilter(m => m.WorkLine.Estimate.CompanyId == CurrentCompanyId);
        });

        var applyFilter = GetType().GetMethod(nameof(ApplyCompanyFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(ICompanyScoped).IsAssignableFrom(t.ClrType)))
        {
            applyFilter.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
        }
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AssignNewRecordsToCurrentCompany();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AssignNewRecordsToCurrentCompany();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    private void ApplyCompanyFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ICompanyScoped =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.CompanyId == CurrentCompanyId);

    // New records always belong to the logged-in user's company, whatever the client sent.
    // Without a logged-in user (registration), the company is set by the code creating it.
    private void AssignNewRecordsToCurrentCompany()
    {
        if (CurrentCompanyId is not { } companyId)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries<ICompanyScoped>().Where(e => e.State == EntityState.Added))
        {
            entry.Property(e => e.CompanyId).CurrentValue = companyId;
        }
    }
}
