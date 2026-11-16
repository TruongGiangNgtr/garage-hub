namespace GarageHub.Common.Dtos.Garage;

public class GarageDto
{
    public int Id { get; set; }
    public int? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal AvgRating { get; set; }
    public int CapacityPerSlot { get; set; }
}
