using System.ComponentModel.DataAnnotations;
using GarageHub.Common.Dtos.Account;
using GarageHub.Common.Dtos.Booking;
using GarageHub.Common.Dtos.Garage;
using GarageHub.Common.Dtos.Vehicle;

namespace GarageHub.Tests;

public class VehicleGarageValidationTests
{
    [Fact]
    public void VehicleInput_RejectsMissingFields()
    {
        var input = new VehicleInputDto
        {
            PlateNumber = "",
            Brand = "",
            Model = ""
        };

        Assert.False(IsValid(input));
    }

    [Fact]
    public void BookingInput_RejectsMissingIdsDateAndSlot()
    {
        var input = new BookingInputDto();

        Assert.False(IsValid(input));
    }

    [Fact]
    public void GoogleProfileCompletion_RequiresAValidNameAndEmail()
    {
        var input = new GoogleProfileCompletionDto
        {
            FullName = "",
            Email = "not-an-email"
        };

        Assert.False(IsValid(input));
    }

    [Fact]
    public void GarageInput_RejectsEmptyRequiredFieldsAndNonpositiveCapacity()
    {
        var input = new GarageInputDto
        {
            Name = " ",
            Address = "",
            Phone = "",
            CapacityPerSlot = 0
        };

        Assert.False(IsValid(input));
    }

    [Fact]
    public void GarageInput_AllowsUnassignedOwnerWhenOtherFieldsAreValid()
    {
        var input = new GarageInputDto
        {
            Name = "Garage",
            Address = "1 Main Street",
            Phone = "0900000000",
            CapacityPerSlot = 1,
            OwnerId = null
        };

        Assert.True(IsValid(input));
    }

    private static bool IsValid(object instance)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
    }
}
