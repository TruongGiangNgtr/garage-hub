using GarageHub.Common.Enums;

namespace GarageHub.Repository.Entities;

public class Reminder
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public DateTime DueDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public ReminderStatus Status { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
}
