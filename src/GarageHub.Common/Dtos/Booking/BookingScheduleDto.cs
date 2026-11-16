using GarageHub.Common.Enums;

namespace GarageHub.Common.Dtos.Booking;

public class BookingScheduleDto
{
    public int Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string GarageName { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public BookingStatus Status { get; set; }
    public decimal DepositAmount { get; set; }
}
