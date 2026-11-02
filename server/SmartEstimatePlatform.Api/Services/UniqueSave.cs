using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Data;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Saves changes that a unique index protects, reporting a duplicate as a domain error both when it already
/// exists and when a concurrent request saves the same values first.
/// </summary>
public static class UniqueSave
{
    /// <summary>
    /// Checks for a duplicate, then saves. If the save fails because a concurrent request won the race,
    /// the duplicate is reported as well; any other failure is rethrown.
    /// </summary>
    public static async Task SaveAsync(AppDbContext db, Func<Task<bool>> duplicateExists, Func<Exception> duplicateError)
    {
        if (await duplicateExists())
        {
            throw duplicateError();
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            // The unique index rejected this save; find out whether that was a duplicate.
            if (await duplicateExists())
            {
                throw duplicateError();
            }

            throw;
        }
    }
}
