using GarageHub.Common.Dtos;
using GarageHub.Common.Dtos.Garage;

namespace GarageHub.Service.Garages;

public interface IGarageService
{
    Task<IReadOnlyList<GarageDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GarageDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserOptionDto>> GetOwnerOptionsAsync(CancellationToken cancellationToken = default);
    Task<int> CreateAsync(GarageInputDto input, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, GarageInputDto input, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
