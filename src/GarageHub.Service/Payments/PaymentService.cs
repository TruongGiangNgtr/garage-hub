using GarageHub.Common.Dtos.Payment;
using GarageHub.Common.Enums;
using GarageHub.Common.Exceptions;
using GarageHub.Common.Helpers;
using GarageHub.Repository.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GarageHub.Service.Payments;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPayOSClient _payOSClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IPayOSClient payOSClient,
        IConfiguration configuration,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _payOSClient = payOSClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> CreatePaymentLinkAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy thanh toán");

        var expiresAt = payment.Booking.ExpiresAt;
        if (payment.Status != PaymentStatus.Pending || expiresAt is null || expiresAt <= VietnamClock.Now)
        {
            throw new ConflictException("Thanh toán không còn hiệu lực hoặc booking đã hết hạn giữ chỗ");
        }

        var baseUrl = _configuration["APP_PUBLIC_BASE_URL"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Thiếu cấu hình APP_PUBLIC_BASE_URL. Đặt bằng dotnet user-secrets.");
        }
        baseUrl = baseUrl.TrimEnd('/');

        // ExpiresAt is Vietnam time (UTC+7).
        var expiredAt = new DateTimeOffset(expiresAt.Value, TimeSpan.FromHours(7)).ToUnixTimeSeconds();

        try
        {
            var link = await _payOSClient.CreatePaymentLinkAsync(
                orderCode: payment.Id,
                amount: decimal.ToInt32(payment.Amount),
                description: "GH" + payment.Id,
                returnUrl: $"{baseUrl}/Payment/Return",
                cancelUrl: $"{baseUrl}/Payment/Cancel",
                expiredAt: expiredAt,
                cancellationToken: cancellationToken);
            return link.CheckoutUrl;
        }
        catch
        {
            // Release the held slot, then surface the original error.
            await _paymentRepository.CancelPendingAsync(paymentId, cancellationToken);
            throw;
        }
    }

    public async Task ConfirmPaymentAsync(long orderCode, string reference, CancellationToken cancellationToken = default)
    {
        if (orderCode is < 1 or > int.MaxValue)
        {
            _logger.LogWarning("PayOS webhook with unknown orderCode {OrderCode}", orderCode);
            return;
        }
        var paymentId = (int)orderCode;

        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            _logger.LogWarning("PayOS webhook for non-existent payment {PaymentId}", paymentId);
            return;
        }
        if (payment.Status == PaymentStatus.Confirmed) return;

        // Ask PayOS directly instead of trusting webhook data. Outside any DB transaction; errors propagate.
        var info = await _payOSClient.GetPaymentStatusAsync(orderCode, cancellationToken);
        if (info.Status != PayOSPaymentStatus.Paid || info.AmountPaid != payment.Amount)
        {
            _logger.LogWarning(
                "Payment {PaymentId} not confirmed: PayOS status {Status}, paid {AmountPaid}, expected {Amount}",
                paymentId, info.Status, info.AmountPaid, payment.Amount);
            return;
        }

        var result = await _paymentRepository.ConfirmAsync(paymentId, reference, VietnamClock.Now, cancellationToken);
        if (result == PaymentConfirmResult.ConfirmedBookingNotPending)
        {
            _logger.LogWarning(
                "Payment {PaymentId} was paid but its booking is no longer Pending; manual handling needed", paymentId);
        }
    }
}
