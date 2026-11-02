namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// Marks an entity that belongs to one company. Queries only ever see the current company's records,
/// and new records are assigned to the current company when saved.
/// </summary>
public interface ICompanyScoped
{
    int CompanyId { get; }
}
