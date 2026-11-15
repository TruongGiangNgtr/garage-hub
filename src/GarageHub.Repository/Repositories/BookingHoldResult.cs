namespace GarageHub.Repository.Repositories;

/// <param name="IsExisting">True when a still-valid Pending booking for the same vehicle and slot was reused.</param>
public record BookingHoldResult(int BookingId, int PaymentId, DateTime ExpiresAt, bool IsExisting);
