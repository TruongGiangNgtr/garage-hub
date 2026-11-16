using Microsoft.Extensions.DependencyInjection;
using GarageHub.Service.Bookings;
using GarageHub.Service.Garages;
using GarageHub.Service.Users;
using GarageHub.Service.Vehicles;

namespace GarageHub.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IGarageService, GarageService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}