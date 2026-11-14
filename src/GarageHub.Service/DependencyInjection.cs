using GarageHub.Service.Users;
using Microsoft.Extensions.DependencyInjection;

namespace GarageHub.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        // Module owners register their services here.
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}