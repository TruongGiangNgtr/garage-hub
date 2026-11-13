using GarageHub.Common.Dtos.Payment;

namespace GarageHub.Service.Payments;

public interface IPayOSClient
{
    /// <param name="expiredAt">Unix time in seconds.</param>
    Task<PayOSPaymentLinkResult> CreatePaymentLinkAsync(
        long orderCode, int amount, string description, string returnUrl, string cancelUrl, long expiredAt,
        CancellationToken cancellationToken = default);

    Task<PayOSPaymentInfo> GetPaymentStatusAsync(long orderCode, CancellationToken cancellationToken = default);

    Task CancelPaymentLinkAsync(long orderCode, string reason, CancellationToken cancellationToken = default);

    /// <summary>Verifies the webhook signature. Throws ValidationException when the body or signature is invalid.</summary>
    Task<PayOSWebhookResult> VerifyWebhookAsync(string body, CancellationToken cancellationToken = default);
}
