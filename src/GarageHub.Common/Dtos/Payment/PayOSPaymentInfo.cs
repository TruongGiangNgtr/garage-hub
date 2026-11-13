namespace GarageHub.Common.Dtos.Payment;

/// <param name="Reference">Bank transaction reference of the paid link; null when PayOS has no transaction yet.</param>
public record PayOSPaymentInfo(PayOSPaymentStatus Status, long AmountPaid, string? Reference);
