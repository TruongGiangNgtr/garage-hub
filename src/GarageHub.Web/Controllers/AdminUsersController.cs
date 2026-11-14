using GarageHub.Common.Enums;
using GarageHub.Common.Exceptions;
using GarageHub.Service.Users;
using GarageHub.Web.Auth;
using GarageHub.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageHub.Web.Controllers;

[Authorize(Policy = AuthPolicies.RequireAdmin)]
[Route("Admin/Users")]
public class AdminUsersController : Controller
{
    private const int PageSize = 20;

    private readonly IUserService _userService;

    public AdminUsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken cancellationToken = default)
        => View(await _userService.GetUsersAsync(search, page, PageSize, cancellationToken));

    [HttpPost("{id:int}/role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(
        int id, UserRole role, string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            await _userService.ChangeRoleAsync(User.GetUserId(), id, role, cancellationToken);
            TempData["Success"] = "Đã cập nhật role";
        }
        catch (Exception ex) when (ex is ConflictException or NotFoundException or ValidationException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { search, page });
    }
}
