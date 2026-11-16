using System.Security.Claims;
using GarageHub.Common.Dtos.Vehicle;
using GarageHub.Common.Exceptions;
using GarageHub.Service.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageHub.Web.Controllers;

[Authorize]
public class VehiclesController : Controller
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _vehicleService.GetAllAsync(CurrentUserId, cancellationToken));

    public IActionResult Create() => View(new VehicleInputDto());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VehicleInputDto input, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _vehicleService.CreateAsync(CurrentUserId, input, cancellationToken);
                TempData["StatusMessage"] = "Vehicle created.";
                return RedirectToAction(nameof(Index));
            }
            catch (ConflictException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return View(input);
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleService.GetByIdAsync(id, CurrentUserId, cancellationToken);
        var input = new VehicleInputDto
        {
            PlateNumber = vehicle.PlateNumber,
            Brand = vehicle.Brand,
            Model = vehicle.Model
        };
        return View(input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VehicleInputDto input, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _vehicleService.UpdateAsync(id, CurrentUserId, input, cancellationToken);
                TempData["StatusMessage"] = "Vehicle updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (ConflictException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return View(input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _vehicleService.DeleteAsync(id, CurrentUserId, cancellationToken);
            TempData["StatusMessage"] = "Vehicle deleted.";
        }
        catch (ConflictException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private int CurrentUserId
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new InvalidOperationException("The authenticated user has an invalid ID claim.");
}
