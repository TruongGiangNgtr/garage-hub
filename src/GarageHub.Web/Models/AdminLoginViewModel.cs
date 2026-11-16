using System.ComponentModel.DataAnnotations;

namespace GarageHub.Web.Models;

public class AdminLoginViewModel
{
    [Required]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
