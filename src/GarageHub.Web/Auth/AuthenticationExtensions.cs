using GarageHub.Common.Enums;

namespace GarageHub.Web.Auth;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Cookie auth + Google login (no ASP.NET Core Identity). Google is registered only when
    /// Authentication:Google:ClientId and ClientSecret are set (dotnet user-secrets), so the app still starts without them.
    /// </summary>
    public static IServiceCollection AddGarageHubAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var builder = services
            .AddAuthentication(AuthSchemes.Cookie)
            .AddCookie(AuthSchemes.Cookie, options =>
            {
                options.Cookie.Name = "GarageHub.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;
                options.Events.OnValidatePrincipal = RoleRefreshValidator.ValidateAsync;
            })
            .AddCookie(AuthSchemes.External, options =>
            {
                options.Cookie.Name = "GarageHub.External";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            });

        var clientId = configuration["Authentication:Google:ClientId"];
        var clientSecret = configuration["Authentication:Google:ClientSecret"];
        if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret))
        {
            builder.AddGoogle(AuthSchemes.Google, options =>
            {
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.SignInScheme = AuthSchemes.External;
                options.Events.OnRemoteFailure = context =>
                {
                    // User pressed Cancel on Google, or the state/correlation check failed.
                    context.Response.Redirect($"{context.Request.PathBase}/Account/Login?failed=true");
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });
        }

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.RequireAdmin, p => p.RequireRole(nameof(UserRole.Admin)))
            .AddPolicy(AuthPolicies.RequireGarageOwner, p => p.RequireRole(nameof(UserRole.GarageOwner)));

        return services;
    }
}
