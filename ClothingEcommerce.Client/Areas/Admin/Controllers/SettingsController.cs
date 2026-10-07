using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;
using WebApplication_ClothingEcommerce.Services.Interfaces;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class SettingsController : Controller
    {
        private readonly IAdminUserService _adminUserService;
        private readonly IStoreSettingsService _storeSettingsService;
        private readonly IWebHostEnvironment _environment;

        public SettingsController(
            IAdminUserService adminUserService,
            IStoreSettingsService storeSettingsService,
            IWebHostEnvironment environment)
        {
            _adminUserService = adminUserService;
            _storeSettingsService = storeSettingsService;
            _environment = environment;
        }

        // =====================================================
        // GET: /Admin/Settings
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            ViewBag.EnvironmentName = _environment.EnvironmentName;
            ViewBag.FrameworkVersion = Environment.Version.ToString();
            ViewBag.ServerTimeUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            ViewBag.IsSuperAdmin = isSuperAdmin;

            var tab = Request.Query["tab"].ToString();
            if (!isSuperAdmin && (tab.Equals("receipt", StringComparison.OrdinalIgnoreCase) || tab.Equals("system", StringComparison.OrdinalIgnoreCase)))
            {
                return RedirectToAction(nameof(Index), new { tab = "store" });
            }

            var settings = await _storeSettingsService.GetSettingsAsync();
            return View(settings);
        }

        // =====================================================
        // POST: /Admin/Settings/UpdateStoreProfile
        // (Accessible to Store Admins & SuperAdmin to manage their store)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStoreProfile(StoreReceiptSettings model)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "Store Admin";
            var success = await _storeSettingsService.UpdateStoreProfileAsync(model, userEmail);

            if (success)
            {
                TempData["Success"] = "Store profile and contact information updated successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update store profile. Please try again.";
            }

            return RedirectToAction(nameof(Index), new { tab = "store" });
        }

        // =====================================================
        // POST: /Admin/Settings/UpdateReceiptSettings
        // (STRICTLY SUPER ADMIN ONLY)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateReceiptSettings(StoreReceiptSettings model)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "SuperAdmin";
            var success = await _storeSettingsService.UpdateReceiptCustomizerAsync(model, userEmail);

            if (success)
            {
                TempData["Success"] = "Receipt customization and default UI template updated successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update receipt settings. Please try again.";
            }

            return RedirectToAction(nameof(Index), new { tab = "receipt" });
        }

        // =====================================================
        // POST: /Admin/Settings/Save (Legacy System Flags)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            string storeName,
            string supportEmail,
            string supportPhone,
            string currency,
            bool maintenanceMode,
            bool allowRegistration)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentEmail = User.FindFirstValue(ClaimTypes.Email);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _adminUserService.SaveSettingsAsync(storeName, supportEmail, supportPhone, currency, maintenanceMode, allowRegistration, currentUserId, currentEmail, ip);

            TempData["Success"] = "System configuration settings saved successfully.";
            return RedirectToAction(nameof(Index), new { tab = "system" });
        }
    }
}
