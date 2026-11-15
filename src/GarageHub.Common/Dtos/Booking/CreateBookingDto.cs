using System.ComponentModel.DataAnnotations;

namespace GarageHub.Common.Dtos.Booking;

public class CreateBookingDto
{
    [Range(1, int.MaxValue)]
    public int VehicleId { get; set; }

    [Range(1, int.MaxValue)]
    public int GarageId { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    /// <summary>Index into BookingSlots.Slots (0..3).</summary>
    [Range(0, 3)]
    public int SlotIndex { get; set; }
}
