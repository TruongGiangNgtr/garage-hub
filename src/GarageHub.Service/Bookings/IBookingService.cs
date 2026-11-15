using GarageHub.Common.Dtos.Booking;

namespace GarageHub.Service.Bookings;

public interface IBookingService
{
    /// <summary>The 4 fixed slots of one day for a garage, with how many seats are taken.</summary>
    Task<IReadOnlyList<SlotAvailabilityDto>> GetSlotAvailabilityAsync(
        int garageId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Holds a slot for 15 minutes (Pending booking + Pending payment) and creates the PayOS deposit link.
    /// Reuses a still-valid Pending booking of the same vehicle and slot (CheckoutUrl is then null).
    /// Throws NotFoundException (vehicle/garage), ValidationException (bad slot), ConflictException (slot full).
    /// </summary>
    Task<BookingHoldDto> CreateBookingAsync(int userId, CreateBookingDto request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingDto>> GetMyBookingsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Customer cancel. Pending/Confirmed -> Canceled (deposit is not refunded); already Canceled is a no-op.
    /// Throws NotFoundException when the booking is not the user's, ConflictException when InProgress/Completed.
    /// </summary>
    Task CancelBookingAsync(int userId, int bookingId, CancellationToken cancellationToken = default);

    /// <summary>Background cleanup: expired Pending bookings and their Pending payments -> Canceled.</summary>
    Task ExpirePendingBookingsAsync(CancellationToken cancellationToken = default);
}
