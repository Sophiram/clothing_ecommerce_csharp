using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin")]
    public class RolesController : Controller
    {
        private readonly IAdminUserService _adminUserService;

        public RolesController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

        // =====================================================
        // GET: /Admin/Roles
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var (roles, roleUserCounts, systemRoles) = await _adminUserService.GetRolesAsync();

            ViewBag.UserCounts = roleUserCounts;
            ViewBag.SystemRoles = systemRoles;

            return View(roles);
        }

        // =====================================================
        // POST: /Admin/Roles/Create
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string roleName)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentEmail = User.FindFirstValue(ClaimTypes.Email);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _adminUserService.CreateRoleAsync(roleName, currentUserId, currentEmail, ip);

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
        // POST: /Admin/Roles/Delete
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentEmail = User.FindFirstValue(ClaimTypes.Email);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _adminUserService.DeleteRoleAsync(id, currentUserId, currentEmail, ip);

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
        // GET: /Admin/Roles/Users?roleName=Admin
        // View all users assigned to a role
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Users(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return NotFound();

            var (role, users, isSystem) = await _adminUserService.GetUsersInRoleAsync(roleName);
            if (role == null) return NotFound();

            ViewBag.Role = role;
            ViewBag.IsSystem = isSystem;

            return View(users);
        }

        // =====================================================
        // POST: /Admin/Roles/Edit
        // Edit custom role name
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string newName)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentEmail = User.FindFirstValue(ClaimTypes.Email);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _adminUserService.EditRoleAsync(id, newName, currentUserId, currentEmail, ip);

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
    }
}
