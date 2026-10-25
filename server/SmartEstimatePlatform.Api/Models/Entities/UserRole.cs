namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// Determines what a user is allowed to see and do within their company.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Manages the company's catalog, projects, estimates, and Viewer accounts.
    /// </summary>
    Manager,

    /// <summary>
    /// Sees only project lists and the names of works and materials.
    /// </summary>
    Viewer
}
