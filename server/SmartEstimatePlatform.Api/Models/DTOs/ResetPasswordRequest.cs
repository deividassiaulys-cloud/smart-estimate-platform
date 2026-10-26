using System.ComponentModel.DataAnnotations;

namespace SmartEstimatePlatform.Api.Models.DTOs;

/// <summary>
/// New password a Manager sets for one of their Viewers.
/// </summary>
public class ResetPasswordRequest
{
    [Required]
    public string NewPassword { get; set; } = string.Empty;
}
