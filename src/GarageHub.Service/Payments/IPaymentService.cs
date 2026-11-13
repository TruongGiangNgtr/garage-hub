namespace GarageHub.Service.Payments;

public interface IPaymentService
{
    /// <summary>Creates a PayOS payment link for a Pending payment and returns the checkout URL.</summary>
    Task<string> CreatePaymentLinkAsync(int paymentId, CancellationToken cancellationToken = default);

    /// <summary>Confirms a payment after a PayOS webhook. Never throws for unknown or already-processed payments.</summary>
    Task ConfirmPaymentAsync(long orderCode, string reference, CancellationToken cancellationToken = default);
}
