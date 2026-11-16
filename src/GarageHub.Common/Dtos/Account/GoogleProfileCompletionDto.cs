using System.ComponentModel.DataAnnotations;

namespace GarageHub.Common.Dtos.Account;

public class GoogleProfileCompletionDto
{
    [Required]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
