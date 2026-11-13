using GarageHub.Common.Dtos.Payment;

namespace GarageHub.Service.Payments;

public interface IPaymentService
{
    /// <summary>Creates a PayOS payment link for a Pending payment and returns the checkout URL.</summary>
    Task<string> CreatePaymentLinkAsync(int paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a payment after a PayOS webhook or a return visit. Never throws for unknown or already-processed payments.
    /// When reference is null it is taken from PayOS.
    /// </summary>
    Task ConfirmPaymentAsync(long orderCode, string? reference, CancellationToken cancellationToken = default);

    /// <summary>Customer came back from PayOS (returnUrl): fall back to asking PayOS, then report the DB state.</summary>
    Task<PaymentResultDto> HandleReturnAsync(long orderCode, CancellationToken cancellationToken = default);

    /// <summary>Customer pressed cancel on PayOS (cancelUrl): cancel only if PayOS confirms the link is Cancelled.</summary>
    Task<PaymentResultDto> HandleCancelAsync(long orderCode, CancellationToken cancellationToken = default);
}
