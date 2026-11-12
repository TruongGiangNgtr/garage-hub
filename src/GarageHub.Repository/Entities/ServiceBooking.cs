using GarageHub.Common.Enums;

namespace GarageHub.Repository.Entities;

public class ServiceBooking
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public int GarageId { get; set; }
    public BookingStatus Status { get; set; }
    /// <summary>Start time of the slot (Vietnam time).</summary>
    public DateTime ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    /// <summary>Hold expiry for Pending bookings.</summary>
    public DateTime? ExpiresAt { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
    public Garage Garage { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public Review? Review { get; set; }
}
