using GarageHub.Service.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Namespace GarageHub.Service (not .Payments) so Program.cs needs no extra using.
namespace GarageHub.Service;

public static class PaymentModuleExtensions
{
    public static IServiceCollection AddPaymentModule(this IServiceCollection services, IConfiguration configuration)
    {
        // PayOS keys are validated lazily on first use, so the app starts without them.
        services.AddSingleton<IPayOSClient, PayOSClientAdapter>();
        services.AddScoped<IPaymentService, PaymentService>();
        return services;
    }
}
