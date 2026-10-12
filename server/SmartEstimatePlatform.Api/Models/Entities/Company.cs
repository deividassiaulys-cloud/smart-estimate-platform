namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// A construction company or contractor that owns its users and all of its data.
/// </summary>
public class Company
{
    public int Id { get; set; }

    /// <summary>
    /// Display name of the company. Not required to be unique across companies.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Accounts that belong to this company.
    /// </summary>
    public ICollection<User> Users { get; set; } = new List<User>();
}
