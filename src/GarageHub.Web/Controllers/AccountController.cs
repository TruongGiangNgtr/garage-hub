using System.Security.Claims;
using GarageHub.Common.Dtos.User;
using GarageHub.Common.Exceptions;
using GarageHub.Service.Users;
using GarageHub.Web.Auth;
using GarageHub.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageHub.Web.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly IAuthenticationSchemeProvider _schemeProvider;

    public AccountController(IUserService userService, IAuthenticationSchemeProvider schemeProvider)
    {
        _userService = userService;
        _schemeProvider = schemeProvider;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl, bool failed = false)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(SafeReturnUrl(returnUrl));
        }

        return View(await BuildLoginModelAsync(returnUrl,
            failed ? "Đăng nhập Google không thành công hoặc đã bị hủy" : null));
    }

    // POST (with antiforgery) so another site cannot start a login on the user's behalf.
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GoogleLogin(string? returnUrl)
    {
        if (await _schemeProvider.GetSchemeAsync(AuthSchemes.Google) is null)
        {
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback), new { returnUrl = SafeReturnUrl(returnUrl) })
        };
        return Challenge(properties, AuthSchemes.Google);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GoogleCallback(string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync(AuthSchemes.External);
        if (!result.Succeeded || result.Principal is null)
        {
            return RedirectToAction(nameof(Login), new { returnUrl, failed = true });
        }

        var external = result.Principal;
        var info = new GoogleUserInfo(
            external.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            external.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            external.FindFirstValue(ClaimTypes.Name) ?? string.Empty);

        UserDto user;
        try
        {
            user = await _userService.SignInWithGoogleAsync(info, cancellationToken);
        }
        catch (Exception ex) when (ex is ConflictException or ValidationException)
        {
            await HttpContext.SignOutAsync(AuthSchemes.External);
            return View(nameof(Login), await BuildLoginModelAsync(returnUrl, ex.Message));
        }

        await HttpContext.SignOutAsync(AuthSchemes.External);
        await HttpContext.SignInAsync(
            AuthSchemes.Cookie,
            AuthClaims.CreatePrincipal(user),
            new AuthenticationProperties { IsPersistent = true });

        return LocalRedirect(SafeReturnUrl(returnUrl));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthSchemes.Cookie);
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();

    private async Task<LoginViewModel> BuildLoginModelAsync(string? returnUrl, string? errorMessage) => new()
    {
        ReturnUrl = SafeReturnUrl(returnUrl),
        GoogleEnabled = await _schemeProvider.GetSchemeAsync(AuthSchemes.Google) is not null,
        ErrorMessage = errorMessage
    };

    private string SafeReturnUrl(string? returnUrl)
        => !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
}
