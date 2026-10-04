using ClothingEcommerce.Client.Services.ApiClient;
using ClothingEcommerce.Shared.DTOs.Catalog;
using ClothingEcommerce.Shared.DTOs.Fulfillment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClothingEcommerce.Client.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager,Staff")]
    public class FulfillmentController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<FulfillmentController> _logger;

        public FulfillmentController(IApiClient apiClient, ILogger<FulfillmentController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // ==========================================
        // GET: /Admin/Fulfillment
        // Warehouse Packing & Dispatch Queue
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(string? tab = "to_pack", string? search = null, int page = 1)
        {
            ViewBag.ActiveTab = tab ?? "to_pack";
            ViewBag.SearchTerm = search;
            ViewBag.Page = page;

            var query = $"api/fulfillment/orders?tab={Uri.EscapeDataString(tab ?? "to_pack")}&page={page}&pageSize=15";
            if (!string.IsNullOrWhiteSpace(search))
            {
                query += $"&search={Uri.EscapeDataString(search)}";
            }

            var response = await _apiClient.GetAsync<PagedResult<FulfillmentOrderDto>>(query);
            var model = response?.Data ?? new PagedResult<FulfillmentOrderDto>();

            return View(model);
        }

        // ==========================================
        // POST: /Admin/Fulfillment/UpdateStatus
        // Mark Order as Packed or Shipped
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateFulfillmentStatusDto request)
        {
            if (request == null)
            {
                return Json(new { success = false, message = "Invalid request payload." });
            }

            var response = await _apiClient.PostAsync<UpdateFulfillmentStatusDto, object>("api/fulfillment/update-status", request);
            if (response == null || !response.Success)
            {
                return Json(new { success = false, message = response?.Message ?? "Failed to update fulfillment status." });
            }

            return Json(new { success = true, message = "Status updated successfully." });
        }
    }
}
