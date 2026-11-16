using GarageHub.Repository.Entities;

namespace GarageHub.Repository.Repositories;

public interface IVehicleRepository
{
    Task<IReadOnlyList<Vehicle>> GetAllWithOwnerAsync(int ownerId, CancellationToken cancellationToken = default);
    Task<Vehicle?> GetTrackedByIdAsync(int id, int ownerId, CancellationToken cancellationToken = default);
    Task<Vehicle?> GetOwnedByIdAsync(int id, int ownerId, CancellationToken cancellationToken = default);
    Task<bool> PlateNumberExistsAsync(string plateNumber, int? exceptVehicleId = null, CancellationToken cancellationToken = default);
    Task<bool> HasRelatedRecordsAsync(int vehicleId, CancellationToken cancellationToken = default);
}
