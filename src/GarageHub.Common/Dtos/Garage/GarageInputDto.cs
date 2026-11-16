using System.ComponentModel.DataAnnotations;

namespace GarageHub.Common.Dtos.Garage;

public class GarageInputDto
{
    public int? OwnerId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CapacityPerSlot { get; set; }
}
