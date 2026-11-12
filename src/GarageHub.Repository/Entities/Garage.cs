namespace GarageHub.Repository.Entities;

public class Garage
{
    public int Id { get; set; }
    public int? OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal AvgRating { get; set; }
    /// <summary>Max bookings accepted per time slot.</summary>
    public int CapacityPerSlot { get; set; }

    public User? Owner { get; set; }
    public ICollection<ServiceBooking> Bookings { get; set; } = new List<ServiceBooking>();
}
