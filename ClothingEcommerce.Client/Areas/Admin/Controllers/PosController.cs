using ClothingEcommerce.Client.Services.ApiClient;
using ClothingEcommerce.Shared.Common;
using ClothingEcommerce.Shared.DTOs.Catalog;
using ClothingEcommerce.Shared.DTOs.Pos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClothingEcommerce.Client.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager,Cashier")]
    public class PosController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<PosController> _logger;

        public PosController(IApiClient apiClient, ILogger<PosController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // ==========================================
        // GET: /Admin/Pos
        // Main POS Terminal Screen
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.CashierName = User.Identity?.Name ?? "Cashier";
            ViewBag.ExchangeRate = 4100;

            // Fetch categories for filter pills
            var catRes = await _apiClient.GetAsync<List<CategoryDto>>("api/categories");
            ViewBag.Categories = catRes?.Data ?? new List<CategoryDto>();

            return View();
        }

        // ==========================================
        // GET: /Admin/Pos/GetProducts
        // API proxy for POS terminal live catalog
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetProducts(string? search = null, Guid? categoryId = null)
        {
            var query = $"api/pos/products?search={Uri.EscapeDataString(search ?? "")}";
            if (categoryId.HasValue && categoryId.Value != Guid.Empty)
            {
                query += $"&categoryId={categoryId.Value}";
            }

            var response = await _apiClient.GetAsync<List<PosProductDto>>(query);
            if (response == null || !response.Success)
            {
                return Json(new { success = false, message = response?.Message ?? "Failed to load products." });
            }

            return Json(new { success = true, data = response.Data });
        }

        // ==========================================
        // POST: /Admin/Pos/Checkout
        // API proxy for processing POS checkout
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> Checkout([FromBody] PosCheckoutRequestDto request)
        {
            if (request == null || request.Items == null || !request.Items.Any())
            {
                return Json(new { success = false, message = "Cart is empty." });
            }

            var response = await _apiClient.PostAsync<PosCheckoutRequestDto, PosCheckoutResponseDto>("api/pos/checkout", request);
            if (response == null || !response.Success)
            {
                return Json(new { 
                    success = false, 
                    message = response?.Message ?? "Checkout failed.", 
                    errors = response?.Errors 
                });
            }

            return Json(new { success = true, data = response.Data });
        }

        // ==========================================
        // GET: /Admin/Pos/DailySummary
        // Cashier shift / register summary
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> DailySummary()
        {
            var response = await _apiClient.GetAsync<PosDailySummaryDto>("api/pos/daily-summary");
            if (response == null || !response.Success)
            {
                return Json(new { success = false, message = response?.Message ?? "Failed to fetch summary." });
            }

            return Json(new { success = true, data = response.Data });
        }
    }
}
