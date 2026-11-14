using GarageHub.Service.Users;
using GarageHub.Web.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace GarageHub.Web.Auth;

/// <summary>
/// Runs on every authenticated request: re-reads the role from the DB so a role change by an Admin
/// applies without logging out. A deleted user is signed out.
/// </summary>
public static class RoleRefreshValidator
{
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (principal is null || !principal.TryGetUserId(out var userId))
        {
            await RejectAsync(context);
            return;
        }

        var services = context.HttpContext.RequestServices;
        try
        {
            var role = await services.GetRequiredService<IUserService>()
                .GetRoleAsync(userId, context.HttpContext.RequestAborted);
            if (role is null)
            {
                await RejectAsync(context);
                return;
            }
            if (principal.GetUserRole() != role)
            {
                context.ReplacePrincipal(AuthClaims.WithRole(principal, role.Value));
                context.ShouldRenew = true;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // DB unavailable: keep the current cookie instead of failing every request (and the error page).
            services.GetRequiredService<ILogger<CookieAuthenticationEvents>>()
                .LogError(ex, "Could not refresh role for user {UserId}", userId);
        }
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(AuthSchemes.Cookie);
    }
}
