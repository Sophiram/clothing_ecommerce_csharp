using ClothingEcommerce.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models.ViewModels;
using WebApplication_ClothingEcommerce.Services;

namespace ClothingEcommerce.Server.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard([FromQuery] string? view = null)
        {
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var data = await _dashboardService.GetDashboardAsync(isSuperAdmin, view);
            return Ok(ApiResponse<AdminDashboardViewModel>.Ok(data));
        }
    }
}
