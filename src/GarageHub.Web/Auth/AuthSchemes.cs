using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

namespace GarageHub.Web.Auth;

public static class AuthSchemes
{
    /// <summary>Main app cookie (default scheme). Holds UserId, Name, Email, Role.</summary>
    public const string Cookie = CookieAuthenticationDefaults.AuthenticationScheme;

    /// <summary>Short-lived cookie that only carries the Google result to AccountController.GoogleCallback.</summary>
    public const string External = "External";

    public const string Google = GoogleDefaults.AuthenticationScheme;
}

public static class AuthPolicies
{
    public const string RequireAdmin = nameof(RequireAdmin);
    public const string RequireGarageOwner = nameof(RequireGarageOwner);
}
