using GarageHub.Common.Dtos.Garage;
using GarageHub.Common.Exceptions;
using GarageHub.Service.Garages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GarageHub.Web.Controllers;

[Authorize(
    AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + AdminController.PasswordCookieScheme,
    Roles = "GarageOwner,Admin")]
public class GaragesController : Controller
{
    private readonly IGarageService _garageService;

    public GaragesController(IGarageService garageService)
    {
        _garageService = garageService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var adminSession = await HttpContext.AuthenticateAsync(AdminController.PasswordCookieScheme);
        ViewBag.HasPasswordAdminSession = adminSession.Succeeded;
        return View(await _garageService.GetAllAsync(cancellationToken));
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadOwnersAsync(null, cancellationToken);
        return View(new GarageInputDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GarageInputDto input, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _garageService.CreateAsync(input, cancellationToken);
                TempData["StatusMessage"] = "Garage created.";
                return RedirectToAction(nameof(Index));
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        await LoadOwnersAsync(input.OwnerId, cancellationToken);
        return View(input);
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var garage = await _garageService.GetByIdAsync(id, cancellationToken);
        var input = new GarageInputDto
        {
            OwnerId = garage.OwnerId,
            Name = garage.Name,
            Address = garage.Address,
            Phone = garage.Phone,
            CapacityPerSlot = garage.CapacityPerSlot
        };
        await LoadOwnersAsync(input.OwnerId, cancellationToken);
        ViewBag.AvgRating = garage.AvgRating;
        return View(input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, GarageInputDto input, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _garageService.UpdateAsync(id, input, cancellationToken);
                TempData["StatusMessage"] = "Garage updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        await LoadOwnersAsync(input.OwnerId, cancellationToken);
        ViewBag.AvgRating = (await _garageService.GetByIdAsync(id, cancellationToken)).AvgRating;
        return View(input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _garageService.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = "Garage deleted.";
        }
        catch (ConflictException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadOwnersAsync(int? selectedOwnerId, CancellationToken cancellationToken)
    {
        var owners = await _garageService.GetOwnerOptionsAsync(cancellationToken);
        ViewBag.Owners = owners.Select(owner => new SelectListItem(
            $"{owner.FullName} ({owner.Email})",
            owner.Id.ToString(),
            owner.Id == selectedOwnerId));
    }
}
