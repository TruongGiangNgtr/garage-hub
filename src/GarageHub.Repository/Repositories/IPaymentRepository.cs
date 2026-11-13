using GarageHub.Repository.Entities;

namespace GarageHub.Repository.Repositories;

public interface IPaymentRepository
{
    /// <summary>Returns the payment with its Booking loaded, or null.</summary>
    Task<Payment?> GetByIdAsync(int paymentId, CancellationToken cancellationToken = default);

    Task<PaymentConfirmResult> ConfirmAsync(int paymentId, string reference, DateTime now, CancellationToken cancellationToken = default);

    /// <summary>Payments Pending -> Canceled, then ServiceBookings Pending -> Canceled. Two separate idempotent updates.</summary>
    Task CancelPendingAsync(int paymentId, CancellationToken cancellationToken = default);
}
