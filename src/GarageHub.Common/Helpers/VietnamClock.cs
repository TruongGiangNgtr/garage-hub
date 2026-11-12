namespace GarageHub.Common.Helpers;

/// <summary>
/// The only place allowed to read the system clock. All stored times are Vietnam time (UTC+7, no DST).
/// </summary>
public static class VietnamClock
{
    public static DateTime Now => DateTime.UtcNow.AddHours(7);
}
