namespace GarageHub.Common.Constants;

public static class BookingSlots
{
    /// <summary>Fixed daily slots (2 hours each), shared by every garage.</summary>
    public static readonly IReadOnlyList<(TimeOnly Start, TimeOnly End)> Slots =
    [
        (new TimeOnly(8, 0), new TimeOnly(10, 0)),
        (new TimeOnly(10, 0), new TimeOnly(12, 0)),
        (new TimeOnly(13, 0), new TimeOnly(15, 0)),
        (new TimeOnly(15, 0), new TimeOnly(17, 0))
    ];

    /// <summary>How long a Pending booking holds its slot before ExpiresAt.</summary>
    public static readonly TimeSpan PendingHoldDuration = TimeSpan.FromMinutes(15);
}
