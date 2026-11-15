using GarageHub.Service.Bookings;
using Microsoft.Extensions.DependencyInjection;

namespace GarageHub.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        // Module owners register their services here.
        services.AddScoped<IBookingService, BookingService>();
        return services;
    }
}