using GarageHub.Common.Enums;

namespace GarageHub.Common.Dtos.Booking;

public record BookingDto(
    int Id,
    int VehicleId,
    string PlateNumber,
    int GarageId,
    string GarageName,
    BookingStatus Status,
    DateTime ScheduledAt,
    DateTime CreatedAt,
    DateTime? ExpiresAt);
