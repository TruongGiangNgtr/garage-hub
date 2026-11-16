using System.Globalization;
using System.Security.Claims;
using GarageHub.Common.Dtos.Booking;
using GarageHub.Service.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageHub.Web.Controllers;

// Views are not part of this module yet (waiting for the wireframe); GET actions return View(model) for the UI owner.
[Authorize]
public class BookingController : Controller
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    // GET /Booking
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _bookingService.GetMyBookingsAsync(GetUserId(), cancellationToken));

    // GET /Booking/Slots?garageId=1&date=2026-11-20
    public async Task<IActionResult> Slots(int garageId, DateOnly date, CancellationToken cancellationToken)
        => View(await _bookingService.GetSlotAvailabilityAsync(garageId, date, cancellationToken));

    // GET /Booking/Create?garageId=1&date=2026-11-20&slotIndex=0
    public IActionResult Create(int garageId, DateOnly date, int slotIndex)
        => View(new CreateBookingDto { GarageId = garageId, Date = date, SlotIndex = slotIndex });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var hold = await _bookingService.CreateBookingAsync(GetUserId(), request, cancellationToken);
        if (hold.CheckoutUrl is not null)
        {
            return Redirect(hold.CheckoutUrl);
        }

        // Reused Pending booking (double submit): its PayOS link was already issued on the first attempt.
        TempData["Message"] = "Bạn đã có lịch đang chờ thanh toán cho ca này, vui lòng hoàn tất thanh toán đã mở trước đó.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayNow(int id, CancellationToken cancellationToken)
    {
        var checkoutUrl = await _bookingService.GetPendingBookingCheckoutUrlAsync(
            GetUserId(), id, cancellationToken);
        return Redirect(checkoutUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        await _bookingService.CancelBookingAsync(GetUserId(), id, cancellationToken);
        TempData["Message"] = "Đã hủy lịch hẹn.";
        return RedirectToAction("Index", "Home");
    }

    /// <summary>Users.Id from the auth cookie (ClaimTypes.NameIdentifier).</summary>
    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId))
        {
            throw new InvalidOperationException("Cookie đăng nhập không có user id hợp lệ.");
        }
        return userId;
    }
}
