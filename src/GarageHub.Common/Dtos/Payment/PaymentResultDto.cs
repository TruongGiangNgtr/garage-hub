using GarageHub.Common.Enums;

namespace GarageHub.Common.Dtos.Payment;

public record PaymentResultDto(PaymentStatus PaymentStatus, BookingStatus BookingStatus, decimal Amount);
