using System.Globalization;
using System.Security.Claims;
using GarageHub.Common.Constants;
using GarageHub.Common.Dtos.Booking;
using GarageHub.Common.Exceptions;
using GarageHub.Common.Helpers;
using GarageHub.Service.Bookings;
using GarageHub.Service.Garages;
using GarageHub.Service.Payments;
using GarageHub.Service.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GarageHub.Web.Controllers;

[Authorize]
public class BookingsController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly IVehicleService _vehicleService;
    private readonly IGarageService _garageService;
    private readonly IPaymentService _paymentService;

    public BookingsController(
        IBookingService bookingService,
        IVehicleService vehicleService,
        IGarageService garageService,
        IPaymentService paymentService)
    {
        _bookingService = bookingService;
        _vehicleService = vehicleService;
        _garageService = garageService;
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(null, cancellationToken);
        return View(new BookingInputDto { ScheduledDate = DateOnly.FromDateTime(VietnamClock.Now) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingInputDto input, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var paymentId = await _bookingService.CreatePendingBookingAsync(
                    CurrentUserId, input, cancellationToken);
                var checkoutUrl = await _paymentService.CreatePaymentLinkAsync(paymentId, cancellationToken);
                return Redirect(checkoutUrl);
            }
            catch (ConflictException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        await LoadOptionsAsync(input.SlotStart, cancellationToken);
        return View(input);
    }

    private async Task LoadOptionsAsync(string? selectedSlot, CancellationToken cancellationToken)
    {
        var vehicles = await _vehicleService.GetAllAsync(CurrentUserId, cancellationToken);
        var garages = await _garageService.GetAllAsync(cancellationToken);
        ViewBag.Vehicles = vehicles.Select(vehicle => new SelectListItem(
            $"{vehicle.PlateNumber} - {vehicle.Brand} {vehicle.Model}", vehicle.Id.ToString()));
        ViewBag.Garages = garages.Select(garage => new SelectListItem(
            garage.Name, garage.Id.ToString()));
        ViewBag.Slots = BookingSlots.Slots.Select(slot =>
        {
            var value = slot.Start.ToString("HH:mm", CultureInfo.InvariantCulture);
            var text = $"{value} - {slot.End.ToString("HH:mm", CultureInfo.InvariantCulture)}";
            return new SelectListItem(text, value, value == selectedSlot);
        });
        ViewBag.MinimumDate = DateOnly.FromDateTime(VietnamClock.Now).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        ViewBag.HasVehicles = vehicles.Count > 0;
        ViewBag.HasGarages = garages.Count > 0;
    }

    private int CurrentUserId
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new InvalidOperationException("The authenticated user has an invalid ID claim.");
}
