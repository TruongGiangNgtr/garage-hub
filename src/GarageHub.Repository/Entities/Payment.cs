using GarageHub.Common.Enums;

namespace GarageHub.Repository.Entities;

public class Payment
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? PayOSTransactionId { get; set; }
    public DateTime? PaidAt { get; set; }

    public ServiceBooking Booking { get; set; } = null!;
}
