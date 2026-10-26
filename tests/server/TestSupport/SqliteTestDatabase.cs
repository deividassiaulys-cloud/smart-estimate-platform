using Microsoft.Data.Sqlite;

namespace SmartEstimatePlatform.Api.Tests.TestSupport;

/// <summary>
/// Opens an in-memory SQLite database that behaves like the MySQL schema for email matching.
/// </summary>
public static class SqliteTestDatabase
{
    // Same name as the MySQL collation in AppDbContext; registered on SQLite with case-insensitive semantics.
    private const string EmailCollation = "utf8mb4_0900_as_ci";

    /// <summary>
    /// Returns an open connection; the database lives as long as the connection stays open.
    /// </summary>
    public static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        connection.CreateCollation(EmailCollation,
            (x, y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase));
        return connection;
    }
}
