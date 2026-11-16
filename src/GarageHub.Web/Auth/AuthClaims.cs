using System.Globalization;
using System.Security.Claims;
using GarageHub.Common.Dtos.User;
using GarageHub.Common.Enums;

namespace GarageHub.Web.Auth;

public static class AuthClaims
{
    public static ClaimsPrincipal CreatePrincipal(UserDto user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthSchemes.Cookie));
    }

    /// <summary>Copy of the principal with the Role claim replaced.</summary>
    public static ClaimsPrincipal WithRole(ClaimsPrincipal principal, UserRole role)
    {
        var identity = new ClaimsIdentity(principal.Claims.Where(c => c.Type != ClaimTypes.Role), AuthSchemes.Cookie);
        identity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));
        return new ClaimsPrincipal(identity);
    }
}
