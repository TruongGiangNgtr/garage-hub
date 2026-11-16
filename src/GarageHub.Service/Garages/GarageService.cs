using GarageHub.Common.Dtos;
using GarageHub.Common.Dtos.Garage;
using GarageHub.Common.Exceptions;
using GarageHub.Repository.Entities;
using GarageHub.Repository.Repositories;

namespace GarageHub.Service.Garages;

public class GarageService : IGarageService
{
    private readonly IGarageRepository _garageRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Garage> _entityRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GarageService(
        IGarageRepository garageRepository,
        IRepository<User> userRepository,
        IRepository<Garage> entityRepository,
        IUnitOfWork unitOfWork)
    {
        _garageRepository = garageRepository;
        _userRepository = userRepository;
        _entityRepository = entityRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<GarageDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var garages = await _garageRepository.GetAllWithOwnerAsync(cancellationToken);
        return garages.Select(garage => ToDto(garage)).ToList();
    }

    public async Task<GarageDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var garage = await _garageRepository.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Garage not found.");
        var ownerName = garage.OwnerId is null
            ? null
            : (await _userRepository.GetByIdAsync(garage.OwnerId.Value, cancellationToken))?.FullName
                ?? throw new NotFoundException("Garage owner not found.");
        return ToDto(garage, ownerName);
    }

    public async Task<IReadOnlyList<UserOptionDto>> GetOwnerOptionsAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users
            .OrderBy(user => user.FullName)
            .Select(user => new UserOptionDto { Id = user.Id, FullName = user.FullName, Email = user.Email })
            .ToList();
    }

    public async Task<int> CreateAsync(GarageInputDto input, CancellationToken cancellationToken = default)
    {
        await ValidateOwnerAsync(input.OwnerId, cancellationToken);
        var garage = new Garage
        {
            OwnerId = input.OwnerId,
            Name = input.Name.Trim(),
            Address = input.Address.Trim(),
            Phone = input.Phone.Trim(),
            CapacityPerSlot = input.CapacityPerSlot
        };
        await _entityRepository.AddAsync(garage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return garage.Id;
    }

    public async Task UpdateAsync(int id, GarageInputDto input, CancellationToken cancellationToken = default)
    {
        var garage = await _garageRepository.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Garage not found.");
        await ValidateOwnerAsync(input.OwnerId, cancellationToken);

        garage.OwnerId = input.OwnerId;
        garage.Name = input.Name.Trim();
        garage.Address = input.Address.Trim();
        garage.Phone = input.Phone.Trim();
        garage.CapacityPerSlot = input.CapacityPerSlot;
        _entityRepository.Update(garage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var garage = await _garageRepository.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Garage not found.");
        if (await _garageRepository.HasBookingsAsync(id, cancellationToken))
        {
            throw new ConflictException("This garage has bookings and cannot be deleted.");
        }

        _entityRepository.Remove(garage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateOwnerAsync(int? ownerId, CancellationToken cancellationToken)
    {
        if (ownerId.HasValue && await _userRepository.GetByIdAsync(ownerId.Value, cancellationToken) is null)
        {
            throw new ValidationException("Select an existing garage owner or leave the garage unassigned.");
        }
    }

    private static GarageDto ToDto(Garage garage, string? ownerName = null)
        => new()
        {
            Id = garage.Id,
            OwnerId = garage.OwnerId,
            OwnerName = ownerName ?? garage.Owner?.FullName,
            Name = garage.Name,
            Address = garage.Address,
            Phone = garage.Phone,
            AvgRating = garage.AvgRating,
            CapacityPerSlot = garage.CapacityPerSlot
        };
}
