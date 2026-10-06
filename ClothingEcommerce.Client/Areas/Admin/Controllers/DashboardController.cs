using ClothingEcommerce.Client.Services.ApiClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace ClothingEcommerce.Client.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    public class DashboardController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IApiClient apiClient, ILogger<DashboardController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? view = null)
        {
            var query = "api/dashboard";
            if (!string.IsNullOrWhiteSpace(view))
            {
                query += $"?view={Uri.EscapeDataString(view)}";
            }

            var response = await _apiClient.GetAsync<AdminDashboardViewModel>(query);
            var model = response?.Data ?? new AdminDashboardViewModel
            {
                IsSuperAdmin = User.IsInRole("SuperAdmin"),
                ActiveView = string.Equals(view, "superadmin", StringComparison.OrdinalIgnoreCase) ? "superadmin" : "store"
            };

            return View(model);
        }
    }
}
