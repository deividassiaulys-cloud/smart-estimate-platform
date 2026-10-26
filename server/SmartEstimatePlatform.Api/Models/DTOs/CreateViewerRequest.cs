using System.ComponentModel.DataAnnotations;

namespace SmartEstimatePlatform.Api.Models.DTOs;

/// <summary>
/// Data a Manager submits to create a Viewer account in their own company.
/// </summary>
public class CreateViewerRequest
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Login email for the Viewer. Must not be in use by any account in any company (case-insensitive).
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password chosen by the Manager and passed on to the Viewer in person. Only its hash is stored.
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}
