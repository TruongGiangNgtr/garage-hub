using GarageHub.Service.Bookings;
using GarageHub.Service.Ocr;
using Microsoft.Extensions.DependencyInjection;

namespace GarageHub.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        // Module owners register their services here.
        services.AddScoped<IBookingService, BookingService>();
        // IPlateRecognizerClient is registered with AddHttpClient in Program.cs (needs IHttpClientFactory from the Web SDK).
        services.AddScoped<IOcrService, OcrService>();
        return services;
    }
}