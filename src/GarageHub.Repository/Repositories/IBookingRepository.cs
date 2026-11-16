using GarageHub.Repository.Entities;

namespace GarageHub.Repository.Repositories;

public interface IBookingRepository
{
    /// <summary>Returns the vehicle's owner id, or null when the vehicle does not exist.</summary>
    Task<int?> GetVehicleOwnerIdAsync(int vehicleId, CancellationToken cancellationToken = default);

    /// <summary>Returns the garage's CapacityPerSlot, or null when the garage does not exist.</summary>
    Task<int?> GetGarageCapacityAsync(int garageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts bookings holding a slot (Confirmed, InProgress, or Pending with ExpiresAt &gt; now) per ScheduledAt
    /// for one garage in [from, to).
    /// </summary>
    Task<IReadOnlyDictionary<DateTime, int>> CountActiveBySlotAsync(
        int garageId, DateTime from, DateTime to, DateTime now, CancellationToken cancellationToken = default);

    /// <summary>
    /// In one transaction: lock the Garage row (UPDLOCK, ROWLOCK), reuse a still-valid Pending booking of the same
    /// vehicle and slot if there is one, otherwise count the slot and insert a Pending booking + Pending payment.
    /// Throws NotFoundException when the garage does not exist and ConflictException when the slot is full.
    /// </summary>
    Task<BookingHoldResult> CreatePendingAsync(
        int vehicleId, int garageId, DateTime scheduledAt, DateTime now, DateTime expiresAt, decimal depositAmount,
        CancellationToken cancellationToken = default);

    /// <summary>Bookings of all vehicles owned by the user, newest slot first (Vehicle and Garage loaded, no tracking).</summary>
    Task<IReadOnlyList<ServiceBooking>> GetByOwnerAsync(int ownerId, CancellationToken cancellationToken = default);

    /// <summary>Returns the booking with its Vehicle loaded (no tracking), or null.</summary>
    Task<ServiceBooking?> GetWithVehicleAsync(int bookingId, CancellationToken cancellationToken = default);

    /// <summary>Ids of the booking's Pending payments.</summary>
    Task<IReadOnlyList<int>> GetPendingPaymentIdsAsync(int bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Conditional cancel in one transaction: booking Pending/Confirmed -> Canceled, then its Pending payments -> Canceled.
    /// Returns false when the booking was not in a cancelable state (nothing changed).
    /// </summary>
    Task<bool> CancelAsync(int bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleanup for the background job, two separate conditional updates (no shared transaction):
    /// Pending payments of expired Pending bookings -> Canceled, then expired Pending bookings -> Canceled.
    /// </summary>
    Task<(int Payments, int Bookings)> ExpirePendingAsync(DateTime now, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceBooking>> GetScheduleForCustomerAsync(
        int customerId, CancellationToken cancellationToken = default);

    Task<int> CreatePendingBookingAsync(
        ServiceBooking booking, Payment payment, DateTime now, CancellationToken cancellationToken = default);
}
