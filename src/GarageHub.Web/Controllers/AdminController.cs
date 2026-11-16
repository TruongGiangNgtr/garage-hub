using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GarageHub.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GarageHub.Web.Controllers;

public class AdminController : Controller
{
    public const string PasswordCookieScheme = "AdminPassword";
    public const string PasswordRateLimitPolicy = "AdminPasswordAttempts";

    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IConfiguration configuration, ILogger<AdminController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
        => View(new AdminLoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(PasswordRateLimitPolicy)]
    public async Task<IActionResult> Login(AdminLoginViewModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var configuredPassword = _configuration["ADMIN_PASSWORD"];
        if (string.IsNullOrWhiteSpace(configuredPassword))
        {
            _logger.LogError("Admin password access is unavailable because ADMIN_PASSWORD is not configured.");
            ModelState.AddModelError(string.Empty, "Admin password access is not configured.");
            return View(input);
        }

        if (!PasswordsMatch(input.Password, configuredPassword))
        {
            ModelState.AddModelError(nameof(input.Password), "The password is incorrect.");
            return View(input);
        }

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "GarageHub Admin"),
            new Claim(ClaimTypes.Role, "Admin")
        ],
        PasswordCookieScheme);
        await HttpContext.SignInAsync(PasswordCookieScheme, new ClaimsPrincipal(identity));

        var destination = Url.IsLocalUrl(input.ReturnUrl)
            ? input.ReturnUrl!
            : Url.Action("Index", "Garages")!;
        return LocalRedirect(destination);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(PasswordCookieScheme);
        return RedirectToAction("Index", "Home");
    }

    private static bool PasswordsMatch(string suppliedPassword, string configuredPassword)
    {
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedPassword);
        var configuredBytes = Encoding.UTF8.GetBytes(configuredPassword);
        return suppliedBytes.Length == configuredBytes.Length
            && CryptographicOperations.FixedTimeEquals(suppliedBytes, configuredBytes);
    }
}
