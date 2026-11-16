using GarageHub.Common.Dtos.Vehicle;
using GarageHub.Common.Exceptions;
using GarageHub.Repository.Entities;
using GarageHub.Repository.Repositories;

namespace GarageHub.Service.Vehicles;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IRepository<Vehicle> _entityRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VehicleService(
        IVehicleRepository vehicleRepository,
        IRepository<Vehicle> entityRepository,
        IUnitOfWork unitOfWork)
    {
        _vehicleRepository = vehicleRepository;
        _entityRepository = entityRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<VehicleDto>> GetAllAsync(int ownerId, CancellationToken cancellationToken = default)
    {
        var vehicles = await _vehicleRepository.GetAllWithOwnerAsync(ownerId, cancellationToken);
        return vehicles.Select(vehicle => ToDto(vehicle)).ToList();
    }

    public async Task<VehicleDto> GetByIdAsync(
        int id, int ownerId, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetOwnedByIdAsync(id, ownerId, cancellationToken)
            ?? throw new NotFoundException("Vehicle not found.");
        return ToDto(vehicle);
    }

    public async Task<int> CreateAsync(
        int ownerId, VehicleInputDto input, CancellationToken cancellationToken = default)
    {
        await ValidateInputAsync(input, null, cancellationToken);

        var vehicle = new Vehicle
        {
            OwnerId = ownerId,
            PlateNumber = input.PlateNumber.Trim(),
            Brand = input.Brand.Trim(),
            Model = input.Model.Trim()
        };
        await _entityRepository.AddAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return vehicle.Id;
    }

    public async Task UpdateAsync(
        int id, int ownerId, VehicleInputDto input, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetTrackedByIdAsync(id, ownerId, cancellationToken)
            ?? throw new NotFoundException("Vehicle not found.");
        await ValidateInputAsync(input, id, cancellationToken);

        vehicle.PlateNumber = input.PlateNumber.Trim();
        vehicle.Brand = input.Brand.Trim();
        vehicle.Model = input.Model.Trim();
        _entityRepository.Update(vehicle);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, int ownerId, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetTrackedByIdAsync(id, ownerId, cancellationToken)
            ?? throw new NotFoundException("Vehicle not found.");
        if (await _vehicleRepository.HasRelatedRecordsAsync(id, cancellationToken))
        {
            throw new ConflictException("This vehicle has bookings or reminders and cannot be deleted.");
        }

        _entityRepository.Remove(vehicle);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateInputAsync(
        VehicleInputDto input, int? exceptVehicleId, CancellationToken cancellationToken)
    {
        var plateNumber = input.PlateNumber.Trim();
        if (await _vehicleRepository.PlateNumberExistsAsync(plateNumber, exceptVehicleId, cancellationToken))
        {
            throw new ConflictException("A vehicle with this plate number already exists.");
        }
    }

    private static VehicleDto ToDto(Vehicle vehicle, string? ownerName = null)
        => new()
        {
            Id = vehicle.Id,
            OwnerId = vehicle.OwnerId,
            OwnerName = ownerName ?? vehicle.Owner.FullName,
            PlateNumber = vehicle.PlateNumber,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            CreatedAt = vehicle.CreatedAt
        };
}
