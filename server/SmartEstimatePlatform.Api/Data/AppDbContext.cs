using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Data;

/// <summary>
/// EF Core database context for the platform's companies and their users.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();

    public DbSet<User> Users => Set<User>();

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
    }
}
