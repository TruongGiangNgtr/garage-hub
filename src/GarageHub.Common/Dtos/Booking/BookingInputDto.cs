using System.ComponentModel.DataAnnotations;

namespace GarageHub.Common.Dtos.Booking;

public class BookingInputDto
{
    [Range(1, int.MaxValue)]
    public int VehicleId { get; set; }

    [Range(1, int.MaxValue)]
    public int GarageId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly? ScheduledDate { get; set; }

    [Required]
    [RegularExpression(@"^\d{2}:\d{2}$")]
    public string SlotStart { get; set; } = string.Empty;
}
