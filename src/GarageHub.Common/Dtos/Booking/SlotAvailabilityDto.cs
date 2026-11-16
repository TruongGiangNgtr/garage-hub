namespace GarageHub.Common.Dtos.Booking;

public record SlotAvailabilityDto(
    int SlotIndex,
    TimeOnly Start,
    TimeOnly End,
    DateTime ScheduledAt,
    int Capacity,
    int Booked,
    bool IsPast)
{
    public int Available => Math.Max(0, Capacity - Booked);
    public bool CanBook => !IsPast && Available > 0;
}
