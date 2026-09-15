using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models.ViewModels;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin")]
    public class AdminsController : Controller
    {
        private readonly IAdminUserService _adminUserService;
        private readonly WebApplication_ClothingEcommerce.Services.Interfaces.IAvatarService _avatarService;

        public AdminsController(
            IAdminUserService adminUserService,
            WebApplication_ClothingEcommerce.Services.Interfaces.IAvatarService avatarService)
        {
            _adminUserService = adminUserService;
            _avatarService = avatarService;
        }

        // =====================================================
        // 1. INDEX: LIST ALL ADMINISTRATORS (Admin & SuperAdmin)
        // GET: /Admin/Admins
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? search = null, string? roleFilter = null)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var adminList = await _adminUserService.GetAdminsAsync(search, roleFilter, currentUserId);
            var (totalAdmins, superAdminCount, adminCount) = await _adminUserService.GetAdminCountsAsync();

            ViewBag.Search = search;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.TotalAdmins = totalAdmins;
            ViewBag.SuperAdminCount = superAdminCount;
            ViewBag.AdminCount = adminCount;
            ViewBag.CurrentUserId = currentUserId;

            return View(adminList);
        }

        // =====================================================
        // 2. CREATE ADMINISTRATOR
        // GET & POST: /Admin/Admins/Create
        // =====================================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new AdminCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || !Request.Headers.Referer.ToString().EndsWith("/Create", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return RedirectToAction(nameof(Index));
                }
                return View(model);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentEmail = User.FindFirstValue(ClaimTypes.Email);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _adminUserService.CreateAdminAsync(model, currentUserId, currentEmail, ip);

            if (!result.Success)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err);
                }
                TempData["Error"] = result.ErrorMessage;
                return View(model);
            }

            TempData["Success"] = result.SuccessMessage;
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // 3. EDIT ADMINISTRATOR
        // GET & POST: /Admin/Admins/Edit
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var vm = await _adminUserService.GetAdminForEditAsync(id);
            if (vm == null) return NotFound();

            ViewBag.AvatarUrl = _avatarService.GetAvatarUrl(id);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminEditViewModel model, IFormFile? avatarFile)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                ViewBag.AvatarUrl = _avatarService.GetAvatarUrl(model.Id);
                return View(model);
            }

            if (avatarFile != null && avatarFile.Length > 0)
            {
                var avatarResult = await _avatarService.UploadAvatarAsync(model.Id, avatarFile);
                if (!avatarResult.Success)
                {
                    TempData["Error"] = string.Join(" ", avatarResult.Errors);
                }
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var currentEmail = User.FindFirstValue(ClaimTypes.Email);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _adminUserService.UpdateAdminAsync(model, currentUserId, currentEmail, ip);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = result.SuccessMessage;
            return RedirectToAction(nameof(Index));
        }

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
        // 4. DETAILS
        // GET: /Admin/Admins/Details/{id}
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var vm = await _adminUserService.GetAdminDetailsAsync(id, currentUserId);
            if (vm == null) return NotFound();

            return View(vm);
        }

        // =====================================================
        // 5. ACTIVATE / DEACTIVATE (TOGGLE STATUS)
        // POST: /Admin/Admins/ToggleStatus
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var currentEmail = User.FindFirstValue(ClaimTypes.Email);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _adminUserService.ToggleAdminStatusAsync(id, currentUserId, currentEmail, ip);

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
        // 6. RESET PASSWORD
        // POST: /Admin/Admins/ResetPassword
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(AdminResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentEmail = User.FindFirstValue(ClaimTypes.Email);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _adminUserService.ResetAdminPasswordAsync(model.Id, model.NewPassword, currentUserId, currentEmail, ip);

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
        // 7. DELETE ADMINISTRATOR
        // POST: /Admin/Admins/Delete
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var currentEmail = User.FindFirstValue(ClaimTypes.Email);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _adminUserService.DeleteAdminAsync(id, currentUserId, currentEmail, ip);

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
        // 8. REVOKE ADMIN ROLE (Demote to standard User)
        // POST: /Admin/Admins/RevokeAdminRole
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeAdminRole(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var currentEmail = User.FindFirstValue(ClaimTypes.Email);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _adminUserService.RevokeAdminRoleAsync(id, currentUserId, currentEmail, ip);

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
        // 9. ASSIGN ADMIN ROLE (Promote existing User/Customer)
        // POST: /Admin/Admins/AssignAdminRole
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAdminRole(string email, string role = "Admin")
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentEmail = User.FindFirstValue(ClaimTypes.Email);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _adminUserService.AssignAdminRoleAsync(email, role, currentUserId, currentEmail, ip);

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
