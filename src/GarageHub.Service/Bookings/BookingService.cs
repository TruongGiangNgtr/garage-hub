using GarageHub.Common.Constants;
using GarageHub.Common.Dtos.Booking;
using GarageHub.Common.Enums;
using GarageHub.Common.Exceptions;
using GarageHub.Common.Helpers;
using GarageHub.Repository.Repositories;
using GarageHub.Service.Payments;
using Microsoft.Extensions.Logging;

namespace GarageHub.Service.Bookings;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IPaymentService _paymentService;
    private readonly IPayOSClient _payOSClient;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IBookingRepository bookingRepository,
        IPaymentService paymentService,
        IPayOSClient payOSClient,
        ILogger<BookingService> logger
    )
    {
        _bookingRepository = bookingRepository;
        _paymentService = paymentService;
        _payOSClient = payOSClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SlotAvailabilityDto>> GetSlotAvailabilityAsync(
        int garageId,
        DateOnly date,
        CancellationToken cancellationToken = default
    )
    {
        var capacity = await _bookingRepository.GetGarageCapacityAsync(garageId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy garage");

        var now = VietnamClock.Now;
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var counts = await _bookingRepository.CountActiveBySlotAsync(garageId, dayStart, dayStart.AddDays(1), now, cancellationToken);

        return BookingSlots.Slots
            .Select((slot, index) =>
            {
                var scheduledAt = date.ToDateTime(slot.Start);
                return new SlotAvailabilityDto(
                    index, slot.Start, slot.End, scheduledAt, capacity,
                    counts.GetValueOrDefault(scheduledAt), IsPast: scheduledAt <= now);
            })
            .ToList();
    }

    public async Task<BookingHoldDto> CreateBookingAsync(
        int userId,
        CreateBookingDto request,
        CancellationToken cancellationToken = default
    )
    {
        if (request.SlotIndex < 0 || request.SlotIndex >= BookingSlots.Slots.Count)
        {
            throw new ValidationException("Ca không hợp lệ");
        }

        var now = VietnamClock.Now;
        var scheduledAt = request.Date.ToDateTime(BookingSlots.Slots[request.SlotIndex].Start);

        if (scheduledAt <= now)
        {
            throw new ValidationException("Ca đã qua, vui lòng chọn ca khác");
        }

        var ownerId = await _bookingRepository.GetVehicleOwnerIdAsync(request.VehicleId, cancellationToken);

        if (ownerId != userId)
        {
            throw new NotFoundException("Không tìm thấy xe");
        }

        // Lock, count and insert happen in one transaction; PayOS is called only after commit.
        var hold = await _bookingRepository.CreatePendingAsync(
            request.VehicleId, request.GarageId, scheduledAt, now, now.Add(BookingSlots.PendingHoldDuration),
            BookingSlots.DepositAmount, cancellationToken);

        if (hold.IsExisting)
        {
            // A PayOS link already exists for this orderCode; creating it again would fail and cancel the hold.
            return new BookingHoldDto(hold.BookingId, hold.PaymentId, hold.ExpiresAt, IsExisting: true, CheckoutUrl: null);
        }

        // On PayOS failure the payment service cancels the payment + booking (releases the slot) and rethrows.
        var checkoutUrl = await _paymentService.CreatePaymentLinkAsync(hold.PaymentId, cancellationToken);

        return new BookingHoldDto(hold.BookingId, hold.PaymentId, hold.ExpiresAt, IsExisting: false, checkoutUrl);
    }

    public async Task<IReadOnlyList<BookingDto>> GetMyBookingsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var bookings = await _bookingRepository.GetByOwnerAsync(userId, cancellationToken);

        return bookings
            .Select(b => new BookingDto(
                b.Id, b.VehicleId, b.Vehicle.PlateNumber, b.GarageId, b.Garage.Name,
                b.Status, b.ScheduledAt, b.CreatedAt, b.ExpiresAt))
            .ToList();
    }

    public async Task CancelBookingAsync(int userId, int bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetWithVehicleAsync(bookingId, cancellationToken);

        if (booking is null || booking.Vehicle.OwnerId != userId)
        {
            throw new NotFoundException("Không tìm thấy lịch hẹn");
        }
        if (booking.Status == BookingStatus.Canceled) return;

        var pendingPaymentIds = await _bookingRepository.GetPendingPaymentIdsAsync(bookingId, cancellationToken);

        if (!await _bookingRepository.CancelAsync(bookingId, cancellationToken))
        {
            // Lost a race: canceled meanwhile (fine) or moved to InProgress/Completed.
            var current = await _bookingRepository.GetWithVehicleAsync(bookingId, cancellationToken);

            if (current?.Status == BookingStatus.Canceled) return;

            throw new ConflictException("Lịch hẹn đang được xử lý hoặc đã hoàn thành, không thể hủy");
        }

        foreach (var paymentId in pendingPaymentIds)
        {
            try
            {
                await _payOSClient.CancelPaymentLinkAsync(paymentId, "Khach huy lich", cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Link may already be canceled/expired; PayOS reports that as an error.
                _logger.LogWarning(ex, "Could not cancel PayOS link for payment {PaymentId}", paymentId);
            }
        }
    }

    public async Task ExpirePendingBookingsAsync(CancellationToken cancellationToken = default)
    {
        var (payments, bookings) = await _bookingRepository.ExpirePendingAsync(VietnamClock.Now, cancellationToken);

        if (payments > 0 || bookings > 0)
        {
            _logger.LogInformation("Expired {Bookings} pending booking(s) and {Payments} pending payment(s)", bookings, payments);
        }
    }
}
