using System.Security.Claims;
using GarageHub.Common.Dtos.Account;
using GarageHub.Common.Exceptions;
using GarageHub.Service.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GarageHub.Web.Controllers;

public class AccountController : Controller
{
    public const string ExternalCookieScheme = "External";

    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;

    public AccountController(IUserService userService, IConfiguration configuration)
    {
        _userService = userService;
        _configuration = configuration;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? error = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Create", "Bookings");
        }

        if (error == "google_login_failed")
        {
            TempData["ErrorMessage"] = "Google sign in failed. Please try again.";
        }

        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Create", "Bookings");
        }
        if (!GoogleIsConfigured)
        {
            TempData["ErrorMessage"] = "Google sign in is not configured. Set GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET in user-secrets.";
            return RedirectToAction(nameof(Login));
        }

        var destination = Url.IsLocalUrl(returnUrl)
            ? returnUrl!
            : Url.Action("Create", "Bookings")!;
        return ChallengeGoogle(destination);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteProfile(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Create", "Bookings");
        }

        var external = await HttpContext.AuthenticateAsync(ExternalCookieScheme);
        var profile = external.Succeeded ? GetGoogleProfile(external.Principal) : null;
        if (profile is null)
        {
            TempData["ErrorMessage"] = "Continue with Google before completing your profile.";
            return RedirectToAction(nameof(Login));
        }

        return View(new GoogleProfileCompletionDto { Email = profile.Email, ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteProfile(
        GoogleProfileCompletionDto input, CancellationToken cancellationToken)
    {
        var external = await HttpContext.AuthenticateAsync(ExternalCookieScheme);
        var profile = external.Succeeded ? GetGoogleProfile(external.Principal) : null;
        if (profile is null)
        {
            TempData["ErrorMessage"] = "Your Google session expired. Please try again.";
            return RedirectToAction(nameof(Login));
        }

        input.Email = profile.Email;
        ModelState.Remove(nameof(input.Email));
        ModelState.Remove(nameof(input.ReturnUrl));
        if (ModelState.IsValid)
        {
            try
            {
                var user = await _userService.CreateGoogleCustomerAsync(
                    profile.GoogleId, profile.Email, input.FullName, cancellationToken);
                await SignInAsync(user);
                await HttpContext.SignOutAsync(ExternalCookieScheme);
                return LocalRedirect(GetBookingDestination(input.ReturnUrl));
            }
            catch (ConflictException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        input.Email = profile.Email;
        return View(input);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(
        string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var external = await HttpContext.AuthenticateAsync(ExternalCookieScheme);
        var profile = external.Succeeded ? GetGoogleProfile(external.Principal) : null;
        if (profile is null)
        {
            await HttpContext.SignOutAsync(ExternalCookieScheme);
            TempData["ErrorMessage"] = "Google sign in did not provide a verified email. Please try again.";
            return RedirectToAction(nameof(Login));
        }

        UserSessionDto? user;
        try
        {
            user = await _userService.FindGoogleAccountAsync(
                profile.GoogleId, profile.Email, profile.FullName, cancellationToken);
        }
        catch (ConflictException ex)
        {
            await HttpContext.SignOutAsync(ExternalCookieScheme);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Login));
        }

        if (user is not null)
        {
            await SignInAsync(user);
            await HttpContext.SignOutAsync(ExternalCookieScheme);
            return LocalRedirect(GetBookingDestination(returnUrl));
        }

        return RedirectToAction(nameof(CompleteProfile), new { returnUrl });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(ExternalCookieScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private IActionResult ChallengeGoogle(string returnUrl)
    {
        var callback = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl })!;
        return Challenge(
            new AuthenticationProperties { RedirectUri = callback },
            GoogleDefaults.AuthenticationScheme);
    }

    private string GetBookingDestination(string? returnUrl)
        => Url.IsLocalUrl(returnUrl)
            ? returnUrl!
            : Url.Action("Create", "Bookings")!;

    private bool GoogleIsConfigured
        => !string.IsNullOrWhiteSpace(_configuration["GOOGLE_CLIENT_ID"])
            && !string.IsNullOrWhiteSpace(_configuration["GOOGLE_CLIENT_SECRET"]);

    private static GoogleProfile? GetGoogleProfile(ClaimsPrincipal? principal)
    {
        var googleId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal?.FindFirstValue(ClaimTypes.Email);
        var fullName = principal?.FindFirstValue(ClaimTypes.Name);
        var emailVerified = principal?.FindFirstValue("urn:google:email_verified");
        if (string.IsNullOrWhiteSpace(googleId)
            || string.IsNullOrWhiteSpace(email)
            || !bool.TryParse(emailVerified, out var verified)
            || !verified)
        {
            return null;
        }

        return new GoogleProfile(googleId, email, fullName ?? string.Empty);
    }

    private async Task SignInAsync(UserSessionDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }

    private sealed record GoogleProfile(string GoogleId, string Email, string FullName);
}
