using GarageHub.Common.Dtos.Booking;

namespace GarageHub.Service.Bookings;

public interface IBookingService
{
    Task<IReadOnlyList<BookingScheduleDto>> GetScheduleForCustomerAsync(
        int customerId, CancellationToken cancellationToken = default);
    Task<int> CreatePendingBookingAsync(
        int customerId, BookingInputDto input, CancellationToken cancellationToken = default);
}
