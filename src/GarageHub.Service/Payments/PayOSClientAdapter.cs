using System.Text.Json;
using GarageHub.Common.Dtos.Payment;
using GarageHub.Common.Exceptions;
using Microsoft.Extensions.Configuration;
using PayOS;
using PayOS.Models;
using PayOS.Models.V2.PaymentRequests;

namespace GarageHub.Service.Payments;

/// <summary>Wraps the payOS SDK so SDK types do not leak out of this class.</summary>
public class PayOSClientAdapter : IPayOSClient
{
    private readonly Lazy<PayOSClient> _client;

    public PayOSClientAdapter(IConfiguration configuration)
    {
        // PublicationOnly: a missing-config exception is not cached, so the app works once keys are set.
        _client = new Lazy<PayOSClient>(() => CreateClient(configuration), LazyThreadSafetyMode.PublicationOnly);
    }

    public async Task<PayOSPaymentLinkResult> CreatePaymentLinkAsync(
        long orderCode, int amount, string description, string returnUrl, string cancelUrl, long expiredAt,
        CancellationToken cancellationToken = default)
    {
        var request = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = amount,
            Description = description,
            ReturnUrl = returnUrl,
            CancelUrl = cancelUrl,
            ExpiredAt = expiredAt
        };
        var response = await _client.Value.PaymentRequests.CreateAsync(request);
        return new PayOSPaymentLinkResult(response.CheckoutUrl, response.PaymentLinkId);
    }

    public async Task<PayOSPaymentInfo> GetPaymentStatusAsync(long orderCode, CancellationToken cancellationToken = default)
    {
        var link = await _client.Value.PaymentRequests.GetAsync(orderCode);
        return new PayOSPaymentInfo(MapStatus(link.Status), link.AmountPaid);
    }

    public async Task CancelPaymentLinkAsync(long orderCode, string reason, CancellationToken cancellationToken = default)
    {
        await _client.Value.PaymentRequests.CancelAsync(orderCode, reason);
    }

    public async Task<PayOSWebhookResult> VerifyWebhookAsync(string body, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _client.Value.Webhooks.VerifyAsync(body);
            return new PayOSWebhookResult(data.OrderCode, data.Amount, data.Reference);
        }
        catch (Exception ex) when (ex is PayOS.Exceptions.PayOSException or JsonException)
        {
            throw new ValidationException("Webhook không hợp lệ");
        }
    }

    private static PayOSClient CreateClient(IConfiguration configuration)
    {
        var clientId = configuration["PAYOS_CLIENT_ID"];
        var apiKey = configuration["PAYOS_API_KEY"];
        var checksumKey = configuration["PAYOS_CHECKSUM_KEY"];

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(clientId)) missing.Add("PAYOS_CLIENT_ID");
        if (string.IsNullOrWhiteSpace(apiKey)) missing.Add("PAYOS_API_KEY");
        if (string.IsNullOrWhiteSpace(checksumKey)) missing.Add("PAYOS_CHECKSUM_KEY");
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Thiếu cấu hình PayOS: {string.Join(", ", missing)}. Đặt bằng dotnet user-secrets.");
        }

        return new PayOSClient(new PayOSOptions { ClientId = clientId, ApiKey = apiKey, ChecksumKey = checksumKey });
    }

    private static PayOSPaymentStatus MapStatus(PaymentLinkStatus status) => status switch
    {
        PaymentLinkStatus.Paid => PayOSPaymentStatus.Paid,
        PaymentLinkStatus.Cancelled => PayOSPaymentStatus.Cancelled,
        PaymentLinkStatus.Underpaid => PayOSPaymentStatus.Underpaid,
        PaymentLinkStatus.Expired => PayOSPaymentStatus.Expired,
        PaymentLinkStatus.Processing => PayOSPaymentStatus.Processing,
        PaymentLinkStatus.Failed => PayOSPaymentStatus.Failed,
        _ => PayOSPaymentStatus.Pending
    };
}
