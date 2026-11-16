using GarageHub.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageHub.Repository.Repositories;

public class GarageRepository : IGarageRepository
{
    private readonly GarageHubDbContext _context;

    public GarageRepository(GarageHubDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Garage>> GetAllWithOwnerAsync(CancellationToken cancellationToken = default)
        => await _context.Garages
            .AsNoTracking()
            .Include(garage => garage.Owner)
            .OrderBy(garage => garage.Name)
            .ToListAsync(cancellationToken);

    public Task<Garage?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Garages.FirstOrDefaultAsync(garage => garage.Id == id, cancellationToken);

    public Task<bool> HasBookingsAsync(int garageId, CancellationToken cancellationToken = default)
        => _context.ServiceBookings.AnyAsync(booking => booking.GarageId == garageId, cancellationToken);
}
