using GarageHub.Service.Bookings;

namespace GarageHub.Web.BackgroundJobs;

/// <summary>
/// Data cleanup only: slot counting already ignores Pending bookings past ExpiresAt,
/// so a slot is released on time even if this job is late.
/// </summary>
public class BookingExpirationJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingExpirationJob> _logger;

    public BookingExpirationJob(IServiceScopeFactory scopeFactory, ILogger<BookingExpirationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                await bookingService.ExpirePendingBookingsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // An unhandled exception here would stop the whole host; log and try again next tick.
                _logger.LogError(ex, "Booking expiration run failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}
