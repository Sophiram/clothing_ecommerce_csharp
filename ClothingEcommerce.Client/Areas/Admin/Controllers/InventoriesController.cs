using ClothingEcommerce.Client.Services.ApiClient;
using ClothingEcommerce.Shared.DTOs.Catalog;
using ClothingEcommerce.Shared.DTOs.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClothingEcommerce.Client.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager,Cashier,Staff")]
    public class InventoriesController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<InventoriesController> _logger;

        public InventoriesController(IApiClient apiClient, ILogger<InventoriesController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // ==========================================
        // GET: /Admin/Inventories
        // GET: /Admin/Inventories/Index
        // ==========================================
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(StockCheck));
        }

        // ==========================================
        // GET: /Admin/Inventories/StockCheck
        // Fast SKU / Barcode Stock Checker for Floor & Warehouse Staff
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> StockCheck([FromQuery] StockFilterDto filter)
        {
            filter.Page = filter.Page < 1 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize < 1 ? 20 : filter.PageSize;

            var queryString = $"api/inventoryadjustment/stock-check?page={filter.Page}&pageSize={filter.PageSize}";
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                queryString += $"&searchTerm={Uri.EscapeDataString(filter.SearchTerm)}";
            }
            if (filter.LowStockOnly == true)
            {
                queryString += $"&lowStockOnly=true&lowStockThreshold={filter.LowStockThreshold}";
            }

            var response = await _apiClient.GetAsync<PagedResult<StockCheckItemDto>>(queryString);
            var model = response?.Data ?? new PagedResult<StockCheckItemDto>();

            // Categories for filter
            var catRes = await _apiClient.GetAsync<List<CategoryDto>>("api/categories");
            ViewBag.Categories = catRes?.Data ?? new List<CategoryDto>();
            ViewBag.Filter = filter;

            return View(model);
        }

        // ==========================================
        // POST: /Admin/Inventories/Adjust
        // Stock IN / Stock OUT (Manager / Admin only)
        // ==========================================
        [Authorize(Roles = "SuperAdmin,Admin,Manager")]
        [HttpPost]
        public async Task<IActionResult> Adjust([FromBody] StockAdjustmentRequestDto request)
        {
            if (request == null)
            {
                return Json(new { success = false, message = "Invalid adjustment request." });
            }

            var response = await _apiClient.PostAsync<StockAdjustmentRequestDto, StockMovementDto>("api/inventoryadjustment/adjust", request);
            if (response == null || !response.Success)
            {
                return Json(new { success = false, message = response?.Message ?? "Adjustment failed.", errors = response?.Errors });
            }

            return Json(new { success = true, data = response.Data, message = "Stock adjusted successfully." });
        }

        // ==========================================
        // GET: /Admin/Inventories/Movements
        // Stock movement audit history (Manager / Admin only)
        // ==========================================
        [Authorize(Roles = "SuperAdmin,Admin,Manager")]
        [HttpGet]
        public async Task<IActionResult> Movements(Guid? variantId = null, string? type = null, int page = 1)
        {
            var query = $"api/inventoryadjustment/movements?page={page}&pageSize=25";
            if (variantId.HasValue && variantId.Value != Guid.Empty) query += $"&variantId={variantId.Value}";
            if (!string.IsNullOrWhiteSpace(type)) query += $"&type={Uri.EscapeDataString(type)}";

            var response = await _apiClient.GetAsync<PagedResult<StockMovementDto>>(query);
            var model = response?.Data ?? new PagedResult<StockMovementDto>();

            ViewBag.VariantId = variantId;
            ViewBag.MovementType = type;

            return View(model);
        }
    }
}
