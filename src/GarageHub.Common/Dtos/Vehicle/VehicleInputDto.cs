using System.ComponentModel.DataAnnotations;

namespace GarageHub.Common.Dtos.Vehicle;

public class VehicleInputDto
{
    [Required]
    [StringLength(20)]
    public string PlateNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Brand { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Model { get; set; } = string.Empty;
}
