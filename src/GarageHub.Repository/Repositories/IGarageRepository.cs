using GarageHub.Repository.Entities;

namespace GarageHub.Repository.Repositories;

public interface IGarageRepository
{
    Task<IReadOnlyList<Garage>> GetAllWithOwnerAsync(CancellationToken cancellationToken = default);
    Task<Garage?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> HasBookingsAsync(int garageId, CancellationToken cancellationToken = default);
}
