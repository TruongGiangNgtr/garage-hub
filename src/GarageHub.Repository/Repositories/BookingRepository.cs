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

    public Task<int?> GetVehicleOwnerIdAsync(int vehicleId, CancellationToken cancellationToken = default)
        => _context.Vehicles
            .Where(v => v.Id == vehicleId)
            .Select(v => (int?)v.OwnerId)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<int?> GetGarageCapacityAsync(int garageId, CancellationToken cancellationToken = default)
        => _context.Garages
            .Where(g => g.Id == garageId)
            .Select(g => (int?)g.CapacityPerSlot)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<DateTime, int>> CountActiveBySlotAsync(
        int garageId, DateTime from, DateTime to, DateTime now, CancellationToken cancellationToken = default)
    {
        return await ActiveBookings(now)
            .Where(b => b.GarageId == garageId && b.ScheduledAt >= from && b.ScheduledAt < to)
            .GroupBy(b => b.ScheduledAt)
            .Select(g => new { ScheduledAt = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ScheduledAt, x => x.Count, cancellationToken);
    }

    public async Task<BookingHoldResult> CreatePendingAsync(
        int vehicleId, int garageId, DateTime scheduledAt, DateTime now, DateTime expiresAt, decimal depositAmount,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        // Serializes every booking attempt on this garage until commit (prevents overbooking the last seat).
        var capacity = await _context.Garages
            .FromSqlInterpolated($"SELECT * FROM Garages WITH (UPDLOCK, ROWLOCK) WHERE Id = {garageId}")
            .Select(g => (int?)g.CapacityPerSlot)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy garage");

        // Double submit: reuse the still-valid Pending booking of the same vehicle and slot.
        var existing = await _context.ServiceBookings
            .Where(b => b.VehicleId == vehicleId && b.GarageId == garageId && b.ScheduledAt == scheduledAt
                && b.Status == BookingStatus.Pending && b.ExpiresAt > now)
            .Select(b => new
            {
                b.Id,
                ExpiresAt = b.ExpiresAt!.Value,
                PaymentId = b.Payments
                    .Where(p => p.Status == PaymentStatus.Pending)
                    .OrderByDescending(p => p.Id)
                    .Select(p => (int?)p.Id)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (existing?.PaymentId is int existingPaymentId)
        {
            await transaction.CommitAsync(cancellationToken);
            return new BookingHoldResult(existing.Id, existingPaymentId, existing.ExpiresAt, IsExisting: true);
        }

        var booked = await ActiveBookings(now)
            .CountAsync(b => b.GarageId == garageId && b.ScheduledAt == scheduledAt, cancellationToken);
        if (booked >= capacity)
        {
            throw new ConflictException("Ca này đã hết chỗ, vui lòng chọn ca khác");
        }

        var booking = new ServiceBooking
        {
            VehicleId = vehicleId,
            GarageId = garageId,
            Status = BookingStatus.Pending,
            ScheduledAt = scheduledAt,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };
        var payment = new Payment
        {
            Booking = booking,
            Amount = depositAmount,
            Status = PaymentStatus.Pending
        };
        _context.ServiceBookings.Add(booking);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return new BookingHoldResult(booking.Id, payment.Id, expiresAt, IsExisting: false);
    }

    public async Task<IReadOnlyList<ServiceBooking>> GetByOwnerAsync(int ownerId, CancellationToken cancellationToken = default)
        => await _context.ServiceBookings
            .AsNoTracking()
            .Include(b => b.Vehicle)
            .Include(b => b.Garage)
            .Where(b => b.Vehicle.OwnerId == ownerId)
            .OrderByDescending(b => b.ScheduledAt)
            .ThenByDescending(b => b.Id)
            .ToListAsync(cancellationToken);

    public Task<ServiceBooking?> GetWithVehicleAsync(int bookingId, CancellationToken cancellationToken = default)
        => _context.ServiceBookings
            .AsNoTracking()
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

    public async Task<IReadOnlyList<int>> GetPendingPaymentIdsAsync(int bookingId, CancellationToken cancellationToken = default)
        => await _context.Payments
            .Where(p => p.BookingId == bookingId && p.Status == PaymentStatus.Pending)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

    public async Task<bool> CancelAsync(int bookingId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var bookingRows = await _context.ServiceBookings
            .Where(b => b.Id == bookingId
                && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, BookingStatus.Canceled), cancellationToken);
        if (bookingRows == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await _context.Payments
            .Where(p => p.BookingId == bookingId && p.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PaymentStatus.Canceled), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<(int Payments, int Bookings)> ExpirePendingAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        var payments = await _context.Payments
            .Where(p => p.Status == PaymentStatus.Pending
                && p.Booking.Status == BookingStatus.Pending && p.Booking.ExpiresAt < now)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PaymentStatus.Canceled), cancellationToken);

        var bookings = await _context.ServiceBookings
            .Where(b => b.Status == BookingStatus.Pending && b.ExpiresAt < now)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, BookingStatus.Canceled), cancellationToken);

        return (payments, bookings);
    }

    /// <summary>Bookings that hold a slot: Confirmed, InProgress, or Pending whose hold has not expired.</summary>
    private IQueryable<ServiceBooking> ActiveBookings(DateTime now)
        => _context.ServiceBookings.Where(b =>
            b.Status == BookingStatus.Confirmed
            || b.Status == BookingStatus.InProgress
            || (b.Status == BookingStatus.Pending && b.ExpiresAt > now));
}
