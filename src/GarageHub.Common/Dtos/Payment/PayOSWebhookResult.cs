namespace GarageHub.Common.Dtos.Payment;

public record PayOSWebhookResult(long OrderCode, long Amount, string Reference);
