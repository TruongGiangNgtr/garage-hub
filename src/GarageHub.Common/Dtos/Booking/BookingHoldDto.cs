namespace GarageHub.Common.Dtos.Booking;

/// <param name="CheckoutUrl">
/// PayOS checkout link. Null when an existing Pending booking was reused (double submit): the link was already
/// issued for that payment and is not stored, so the customer must finish it from the first attempt.
/// </param>
public record BookingHoldDto(int BookingId, int PaymentId, DateTime ExpiresAt, bool IsExisting, string? CheckoutUrl);
