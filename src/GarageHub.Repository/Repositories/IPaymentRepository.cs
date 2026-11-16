using GarageHub.Repository.Entities;

namespace GarageHub.Repository.Repositories;

public interface IPaymentRepository
{
    /// <summary>Returns the payment with its Booking loaded (no tracking), or null.</summary>
    Task<Payment?> GetByIdAsync(int paymentId, CancellationToken cancellationToken = default);

    /// <param name="reference">Bank transaction reference; may be null when PayOS did not provide one.</param>
    Task<PaymentConfirmResult> ConfirmAsync(int paymentId, string? reference, DateTime now, CancellationToken cancellationToken = default);

    /// <summary>Replaces a canceled or Pending payment with a new Pending payment for the same active booking.</summary>
    Task<int> CreateReplacementPendingAsync(int paymentId, DateTime now, CancellationToken cancellationToken = default);

    /// <summary>Marks a payment canceled without releasing its booking hold.</summary>
    Task CancelPaymentOnlyAsync(int paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Payments Pending -> Canceled, then the booking Pending -> Canceled only if it has no other Pending payment.
    /// Two separate idempotent updates (no shared transaction).
    /// </summary>
    Task CancelPendingAsync(int paymentId, CancellationToken cancellationToken = default);
}
