using GarageHub.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageHub.Repository.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly GarageHubDbContext _context;

    public VehicleRepository(GarageHubDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Vehicle>> GetAllWithOwnerAsync(
        int ownerId, CancellationToken cancellationToken = default)
        => await _context.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.Owner)
            .Where(vehicle => vehicle.OwnerId == ownerId)
            .OrderBy(vehicle => vehicle.PlateNumber)
            .ToListAsync(cancellationToken);

    public Task<Vehicle?> GetTrackedByIdAsync(int id, int ownerId, CancellationToken cancellationToken = default)
        => _context.Vehicles.FirstOrDefaultAsync(
            vehicle => vehicle.Id == id && vehicle.OwnerId == ownerId, cancellationToken);

    public Task<Vehicle?> GetOwnedByIdAsync(int id, int ownerId, CancellationToken cancellationToken = default)
        => _context.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.Owner)
            .FirstOrDefaultAsync(vehicle => vehicle.Id == id && vehicle.OwnerId == ownerId, cancellationToken);

    public Task<bool> PlateNumberExistsAsync(
        string plateNumber, int? exceptVehicleId = null, CancellationToken cancellationToken = default)
        => _context.Vehicles.AnyAsync(
            vehicle => vehicle.PlateNumber == plateNumber
                && (!exceptVehicleId.HasValue || vehicle.Id != exceptVehicleId.Value),
            cancellationToken);

    public async Task<bool> HasRelatedRecordsAsync(int vehicleId, CancellationToken cancellationToken = default)
    {
        var hasBookings = await _context.ServiceBookings
            .AnyAsync(booking => booking.VehicleId == vehicleId, cancellationToken);
        return hasBookings || await _context.Reminders
            .AnyAsync(reminder => reminder.VehicleId == vehicleId, cancellationToken);
    }
}
