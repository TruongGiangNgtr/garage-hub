using System.Net;
using GarageHub.Common.Exceptions;

namespace GarageHub.Web.Middleware;

public class ExceptionHandlingMiddleware
{
    private const string GenericMessage = "Đã có lỗi xảy ra";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex, "Exception after response started on {Path}", context.Request.Path);
                throw;
            }

            var status = MapStatus(ex);
            if (status == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            }
            else
            {
                _logger.LogWarning("{ExceptionType} on {Path}: {Message}", ex.GetType().Name, context.Request.Path, ex.Message);
            }

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                var message = status != HttpStatusCode.InternalServerError ? ex.Message
                    : _environment.IsDevelopment() ? ex.ToString()
                    : GenericMessage;

                context.Response.Clear();
                context.Response.StatusCode = (int)status;
                await context.Response.WriteAsJsonAsync(new { status = (int)status, message });
            }
            else
            {
                context.Response.Redirect("/Home/Error");
            }
        }
    }

    private static HttpStatusCode MapStatus(Exception ex) => ex switch
    {
        NotFoundException => HttpStatusCode.NotFound,
        ConflictException => HttpStatusCode.Conflict,
        ValidationException => HttpStatusCode.BadRequest,
        _ => HttpStatusCode.InternalServerError
    };
}
