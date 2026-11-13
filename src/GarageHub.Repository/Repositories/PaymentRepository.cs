using GarageHub.Common.Enums;
using GarageHub.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageHub.Repository.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly GarageHubDbContext _context;

    public PaymentRepository(GarageHubDbContext context)
    {
        _context = context;
    }

    public Task<Payment?> GetByIdAsync(int paymentId, CancellationToken cancellationToken = default)
        => _context.Payments
            .AsNoTracking()
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

    public async Task<PaymentConfirmResult> ConfirmAsync(
        int paymentId, string? reference, DateTime now, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var paymentRows = await _context.Payments
            .Where(p => p.Id == paymentId && p.Status != PaymentStatus.Confirmed)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, PaymentStatus.Confirmed)
                .SetProperty(p => p.PaidAt, now)
                .SetProperty(p => p.PayOSTransactionId, reference), cancellationToken);
        if (paymentRows == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return PaymentConfirmResult.AlreadyProcessed;
        }

        var bookingId = await _context.Payments
            .Where(p => p.Id == paymentId)
            .Select(p => p.BookingId)
            .SingleAsync(cancellationToken);

        var bookingRows = await _context.ServiceBookings
            .Where(b => b.Id == bookingId && b.Status == BookingStatus.Pending && b.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, BookingStatus.Confirmed), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return bookingRows == 0 ? PaymentConfirmResult.ConfirmedBookingNotPending : PaymentConfirmResult.Confirmed;
    }

    public async Task CancelPendingAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        var bookingId = await _context.Payments
            .Where(p => p.Id == paymentId)
            .Select(p => (int?)p.BookingId)
            .SingleOrDefaultAsync(cancellationToken);

        await _context.Payments
            .Where(p => p.Id == paymentId && p.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PaymentStatus.Canceled), cancellationToken);

        if (bookingId is null) return;

        await _context.ServiceBookings
            .Where(b => b.Id == bookingId && b.Status == BookingStatus.Pending
                && !_context.Payments.Any(p => p.BookingId == b.Id && p.Status == PaymentStatus.Pending))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, BookingStatus.Canceled), cancellationToken);
    }
}
