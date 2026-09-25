using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // =========================================================
        // DASHBOARD INDEX
        // GET: /Admin/Dashboard?view=superadmin OR ?view=store
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? view = null)
        {
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var model = await _dashboardService.GetDashboardAsync(isSuperAdmin, view);

            return View(model);
        }
    }
}