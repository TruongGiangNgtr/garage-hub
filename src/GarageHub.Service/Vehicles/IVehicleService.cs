using GarageHub.Common.Dtos.Vehicle;

namespace GarageHub.Service.Vehicles;

public interface IVehicleService
{
    Task<IReadOnlyList<VehicleDto>> GetAllAsync(int ownerId, CancellationToken cancellationToken = default);
    Task<VehicleDto> GetByIdAsync(int id, int ownerId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(int ownerId, VehicleInputDto input, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, int ownerId, VehicleInputDto input, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, int ownerId, CancellationToken cancellationToken = default);
}
