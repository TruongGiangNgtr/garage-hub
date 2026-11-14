using System.Globalization;
using System.Security.Claims;
using GarageHub.Common.Enums;

namespace GarageHub.Web.Extensions;

/// <summary>
/// Read the signed-in user inside controllers/views: <c>User.GetUserId()</c>, <c>User.GetUserRole()</c>.
/// The id is Users.Id in our database (not the Google id).
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Throws when the request is not authenticated; use on actions marked [Authorize].</summary>
    public static int GetUserId(this ClaimsPrincipal principal)
        => principal.TryGetUserId(out var userId)
            ? userId
            : throw new InvalidOperationException("Người dùng chưa đăng nhập");

    public static bool TryGetUserId(this ClaimsPrincipal principal, out int userId)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out userId);
    }

    public static UserRole? GetUserRole(this ClaimsPrincipal principal)
        => Enum.TryParse<UserRole>(principal.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;

    public static string? GetEmail(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Email);
}
