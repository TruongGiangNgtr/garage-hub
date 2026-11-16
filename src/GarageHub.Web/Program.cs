using System.Security.Claims;
using System.Threading.RateLimiting;
using GarageHub.Repository;
using GarageHub.Service;
using GarageHub.Service.Ocr;
using GarageHub.Web.BackgroundJobs;
using GarageHub.Web.Auth;
using GarageHub.Web.Controllers;
using GarageHub.Web.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
var authentication = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
    })
    .AddCookie(AccountController.ExternalCookieScheme, options =>
    {
        options.Cookie.Name = "GarageHub.External";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddCookie(AdminController.PasswordCookieScheme, options =>
    {
        options.Cookie.Name = "GarageHub.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.LoginPath = "/Admin/Login";
        options.SlidingExpiration = false;
    });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AdminController.PasswordRateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(5)
            }));
});

var googleClientId = builder.Configuration["GOOGLE_CLIENT_ID"];
var googleClientSecret = builder.Configuration["GOOGLE_CLIENT_SECRET"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authentication.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SignInScheme = AccountController.ExternalCookieScheme;
        options.Events.OnRemoteFailure = async context =>
        {
            await context.HttpContext.SignOutAsync(AccountController.ExternalCookieScheme);
            context.Response.Redirect("/Account/Login?error=google_login_failed");
            context.HandleResponse();
        };
        options.Events.OnCreatingTicket = context =>
        {
            var emailVerified =
                (context.User.TryGetProperty("email_verified", out var emailVerifiedClaim)
                    && emailVerifiedClaim.ValueKind == System.Text.Json.JsonValueKind.True)
                || (context.User.TryGetProperty("verified_email", out var verifiedEmailClaim)
                    && verifiedEmailClaim.ValueKind == System.Text.Json.JsonValueKind.True);
            context.Identity?.AddClaim(new Claim("urn:google:email_verified", emailVerified.ToString()));
            return Task.CompletedTask;
        };
    });
}
builder.Services.AddAuthorization();
builder.Services.AddRepositoryLayer(builder.Configuration);
builder.Services.AddServiceLayer();
builder.Services.AddPaymentModule(builder.Configuration);
builder.Services.AddHostedService<BookingExpirationJob>();
builder.Services.AddHttpClient<IPlateRecognizerClient, PlateRecognizerClient>();
builder.Services.AddGarageHubAuthentication(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseGlobalExceptionHandling();

if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
