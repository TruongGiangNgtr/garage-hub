namespace GarageHub.Web.Models;

public class LoginViewModel
{
    public string? ReturnUrl { get; set; }

    /// <summary>False when the Google keys are not configured on this machine.</summary>
    public bool GoogleEnabled { get; set; }

    public string? ErrorMessage { get; set; }
}
