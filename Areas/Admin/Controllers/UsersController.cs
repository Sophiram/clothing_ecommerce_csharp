using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class UsersController : Controller
{
    private readonly IAdminUserService _adminUserService;
    private readonly WebApplication_ClothingEcommerce.Services.Interfaces.IAvatarService _avatarService;

    public UsersController(
        IAdminUserService adminUserService,
        WebApplication_ClothingEcommerce.Services.Interfaces.IAvatarService avatarService)
    {
        _adminUserService = adminUserService;
        _avatarService = avatarService;
    }

    // =====================================================
    // GET: Admin/Users
    // =====================================================
    public async Task<IActionResult> Index(string? search = null, string? roleFilter = null)
    {
        var (filteredUsers, userRolesMap, userLockoutMap, userAvatarsMap, allRoles) = await _adminUserService.GetUsersAsync(search, roleFilter);

        ViewBag.Search = search;
        ViewBag.RoleFilter = roleFilter;
        ViewBag.UserRoles = userRolesMap;
        ViewBag.UserLockout = userLockoutMap;
        ViewBag.UserAvatars = userAvatarsMap;
        ViewBag.AllRoles = allRoles;
        ViewBag.CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return View(filteredUsers);
    }

    // =====================================================
    // GET: Admin/Users/Details/5
    // =====================================================
    public async Task<IActionResult> Details(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(id)) return NotFound();
        }

        var (user, roles) = await _adminUserService.GetUserDetailsAsync(id);
        if (user == null) return NotFound();

        var isLocked = await _adminUserService.GetUserLockoutStatusAsync(id);

        ViewBag.Roles = roles;
        ViewBag.IsCurrent = (user.Id == User.FindFirstValue(ClaimTypes.NameIdentifier));
        ViewBag.IsLocked = isLocked;
        ViewBag.LockoutEnd = user.LockoutEnd;
        ViewBag.AccessFailedCount = user.AccessFailedCount;
        ViewBag.AvatarUrl = _avatarService.GetAvatarUrl(user.Id);
        return View(user);
    }

    // =====================================================
    // GET: Admin/Users/Create
    // =====================================================
    public async Task<IActionResult> Create()
    {
        var roles = await _adminUserService.GetAllRoleNamesAsync();
        ViewBag.Roles = new SelectList(roles, "User");
        return View();
    }

    // =====================================================
    // POST: Admin/Users/Create
    // =====================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string firstName, string lastName, string email, string password, string role)
    {
        if (ModelState.IsValid)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentEmail = User.FindFirstValue(ClaimTypes.Email);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _adminUserService.CreateUserAsync(firstName, lastName, email, password, role, currentUserId, currentEmail, ip);

            if (result.Success)
            {
                TempData["Success"] = result.SuccessMessage;
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
        }

        var roles = await _adminUserService.GetAllRoleNamesAsync();
        ViewBag.Roles = new SelectList(roles, role);
        return View();
    }

    // =====================================================
    // GET: Admin/Users/Edit/5
    // =====================================================
    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var (user, currentRoles) = await _adminUserService.GetUserForEditAsync(id);
        if (user == null) return NotFound();

        var roles = await _adminUserService.GetAllRoleNamesAsync();
        ViewBag.Roles = new SelectList(roles, currentRoles.FirstOrDefault() ?? "User");
        ViewBag.AvatarUrl = _avatarService.GetAvatarUrl(user.Id);

        return View(user);
    }

    // =====================================================
    // POST: Admin/Users/Edit/5
    // =====================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, string firstName, string lastName, string email, string? phoneNumber, string role, IFormFile? avatarFile)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var currentEmail = User.FindFirstValue(ClaimTypes.Email);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (avatarFile != null && avatarFile.Length > 0)
        {
            var avatarResult = await _avatarService.UploadAvatarAsync(id, avatarFile);
            if (!avatarResult.Success)
            {
                TempData["Error"] = string.Join(" ", avatarResult.Errors);
            }
        }

        var result = await _adminUserService.UpdateUserAsync(id, firstName, lastName, email, phoneNumber, role, currentUserId, currentEmail, ip);

        if (result.Success)
        {
            TempData["Success"] = result.SuccessMessage;
            return RedirectToAction(nameof(Index));
        }

        if (result.ErrorMessage == "User not found.")
        {
            return NotFound();
        }

        TempData["Error"] = result.ErrorMessage;
        var roles = await _adminUserService.GetAllRoleNamesAsync();
        ViewBag.Roles = new SelectList(roles, role);
        ViewBag.AvatarUrl = _avatarService.GetAvatarUrl(id);

        var (user, _) = await _adminUserService.GetUserForEditAsync(id);
        return View(user);
    }

    // =====================================================
    // POST: Admin/Users/RemoveAvatar
    // =====================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAvatar(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();
        await _avatarService.DeleteAvatarAsync(id);
        TempData["Success"] = "Profile picture removed.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    // =====================================================
    // GET: Admin/Users/Delete/5
    // =====================================================
    public async Task<IActionResult> Delete(string? id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var (user, roles) = await _adminUserService.GetUserDetailsAsync(id);
        if (user == null) return NotFound();

        ViewBag.Roles = roles;
        return View(user);
    }

    // =====================================================
    // POST: Admin/Users/Delete/5
    // =====================================================
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var currentEmail = User.FindFirstValue(ClaimTypes.Email);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _adminUserService.DeleteUserAsync(id, currentUserId, currentEmail, ip);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = result.SuccessMessage;
        }

        return RedirectToAction(nameof(Index));
    }

    // =====================================================
    // POST: Admin/Users/ToggleStatus
    // =====================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var currentEmail = User.FindFirstValue(ClaimTypes.Email);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _adminUserService.ToggleUserStatusAsync(id, currentUserId, currentEmail, ip);

        if (!result.Success)
        {
            if (result.ErrorMessage == "User not found.") return NotFound();
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = result.SuccessMessage;
        }

        return RedirectToAction(nameof(Index));
    }

    // =====================================================
    // POST: Admin/Users/ResetPassword
    // =====================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id, string newPassword, string confirmPassword)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentEmail = User.FindFirstValue(ClaimTypes.Email);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _adminUserService.ResetUserPasswordAsync(id, newPassword, confirmPassword, currentUserId, currentEmail, ip);

        if (!result.Success)
        {
            if (result.ErrorMessage == "User not found.") return NotFound();
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = result.SuccessMessage;
        }

        return RedirectToAction(nameof(Index));
    }
}