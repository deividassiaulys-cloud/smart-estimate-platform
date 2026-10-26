using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Data;

/// <summary>
/// EF Core database context for the platform's companies and their users.
/// Every <see cref="ICompanyScoped"/> entity is filtered to the current company, so another company's
/// records are never visible. Only AuthService may bypass this, with IgnoreQueryFilters.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentCompany? currentCompany = null)
    : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();

    public DbSet<User> Users => Set<User>();

    // Read by the query filters each time a query runs, so each request sees only its own company.
    private int? CurrentCompanyId => currentCompany?.CompanyId;

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
            entry.Entity.CompanyId = companyId;
        }
    }
}
