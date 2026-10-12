using System.ComponentModel.DataAnnotations;

namespace SmartEstimatePlatform.Api.Models.DTOs;

/// <summary>
/// Data a visitor submits to register; creates their company and their Manager account together.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Name of the company to create. Not required to be unique across companies.
    /// </summary>
    [Required]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Login email for the new Manager account. Must not already be in use (case-insensitive).
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Plain-text password. Only its hash is stored; weak passwords are rejected.
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}
