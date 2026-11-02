namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// A client job that groups the company's estimates. Names need not be unique; the description tells
/// projects with the same name apart.
/// </summary>
public class Project : ICompanyScoped
{
    // Used by EF Core when loading a project.
    private Project()
    {
    }

    /// <summary>
    /// Creates a project with an already validated and trimmed name and description.
    /// </summary>
    public Project(string name, string? description)
    {
        Update(name, description);
    }

    public int Id { get; private set; }

    /// <summary>
    /// Trimmed name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Trimmed description, or null when there is none.
    /// </summary>
    public string? Description { get; private set; }

    public int CompanyId { get; private set; }

    public Company Company { get; private set; } = null!;

    /// <summary>
    /// Replaces the name and description with already validated and trimmed values.
    /// </summary>
    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }
}
