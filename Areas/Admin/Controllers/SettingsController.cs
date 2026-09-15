using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin")]
    public class SettingsController : Controller
    {
        private readonly IAdminUserService _adminUserService;
        private readonly IWebHostEnvironment _environment;

        public SettingsController(
            IAdminUserService adminUserService,
            IWebHostEnvironment environment)
        {
            _adminUserService = adminUserService;
            _environment = environment;
        }

        // =====================================================
        // GET: /Admin/Settings
        // =====================================================
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.EnvironmentName = _environment.EnvironmentName;
            ViewBag.FrameworkVersion = Environment.Version.ToString();
            ViewBag.ServerTimeUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            return View();
        }

        // =====================================================
        // POST: /Admin/Settings/Save
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
            return RedirectToAction(nameof(Index));
        }
    }
}
