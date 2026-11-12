namespace GarageHub.Repository.Entities;

public class Vehicle
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public User Owner { get; set; } = null!;
    public ICollection<ServiceBooking> Bookings { get; set; } = new List<ServiceBooking>();
    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}
