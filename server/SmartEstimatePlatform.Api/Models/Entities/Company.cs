namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// A construction company or contractor that owns its users and all of its data.
/// </summary>
public class Company
{
    // Used by EF Core when loading a company.
    private Company()
    {
    }

    /// <summary>
    /// Creates a company with the given display name.
    /// </summary>
    public Company(string name)
    {
        Name = name;
    }

    public int Id { get; private set; }

    /// <summary>
    /// Display name of the company. Not required to be unique across companies.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Accounts that belong to this company.
    /// </summary>
    public ICollection<User> Users { get; private set; } = new List<User>();
}
