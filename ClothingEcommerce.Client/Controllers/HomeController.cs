using System.Diagnostics;
using ClothingEcommerce.Client.Services.ApiClient;
using ClothingEcommerce.Shared.DTOs.Catalog;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;

namespace ClothingEcommerce.Client.Controllers
{
    public class HomeController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IApiClient apiClient, ILogger<HomeController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var featuredResponse = await _apiClient.GetAsync<List<ProductDto>>("api/products/featured?count=8");
            var categoriesResponse = await _apiClient.GetAsync<List<CategoryDto>>("api/categories");

            ViewBag.FeaturedProducts = featuredResponse?.Data ?? new List<ProductDto>();
            ViewBag.Categories = categoriesResponse?.Data ?? new List<CategoryDto>();

            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
