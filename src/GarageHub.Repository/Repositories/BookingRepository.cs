using System.Data;
using GarageHub.Common.Enums;
using GarageHub.Common.Exceptions;
using GarageHub.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageHub.Repository.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly GarageHubDbContext _context;

    public BookingRepository(GarageHubDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ServiceBooking>> GetScheduleForCustomerAsync(
        int customerId, CancellationToken cancellationToken = default)
        => await _context.ServiceBookings
            .AsNoTracking()
            .Include(booking => booking.Vehicle)
            .Include(booking => booking.Garage)
            .Include(booking => booking.Payments)
            .Where(booking => booking.Vehicle.OwnerId == customerId)
            .OrderByDescending(booking => booking.ScheduledAt)
            .ToListAsync(cancellationToken);

    public async Task<int> CreatePendingBookingAsync(
        ServiceBooking booking, Payment payment, DateTime now, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var capacity = await _context.Garages
            .Where(garage => garage.Id == booking.GarageId)
            .Select(garage => (int?)garage.CapacityPerSlot)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Garage not found.");

        var activeBookings = await _context.ServiceBookings.CountAsync(existing =>
            existing.GarageId == booking.GarageId
            && existing.ScheduledAt == booking.ScheduledAt
            && (existing.Status == BookingStatus.Confirmed
                || existing.Status == BookingStatus.InProgress
                || (existing.Status == BookingStatus.Pending && existing.ExpiresAt > now)),
            cancellationToken);

        if (activeBookings >= capacity)
        {
            throw new ConflictException("This time slot is full. Choose another slot.");
        }

        _context.ServiceBookings.Add(booking);
        await _context.SaveChangesAsync(cancellationToken);

        payment.BookingId = booking.Id;
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return payment.Id;
    }
}
