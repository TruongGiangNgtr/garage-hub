namespace GarageHub.Repository.Entities;

public class Review
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    public ServiceBooking Booking { get; set; } = null!;
}
