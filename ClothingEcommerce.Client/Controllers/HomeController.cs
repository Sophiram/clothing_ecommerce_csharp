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
            var brandsResponse = await _apiClient.GetAsync<List<BrandDto>>("api/brands");

            var categories = categoriesResponse?.Data?.Select(c => new Category
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            }).ToList() ?? new List<Category>();

            var brands = brandsResponse?.Data?.Select(b => new Brand
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description
            }).ToList() ?? new List<Brand>();

            var products = featuredResponse?.Data?.Select(p => new Product
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CategoryId = p.CategoryId ?? Guid.Empty,
                BrandId = p.BrandId ?? Guid.Empty,
                Category = new Category { Id = p.CategoryId ?? Guid.Empty, Name = p.CategoryName ?? "" },
                Brand = new Brand { Id = p.BrandId ?? Guid.Empty, Name = p.BrandName ?? "" },
                Images = !string.IsNullOrEmpty(p.PrimaryImageUrl)
                    ? new List<ProductImage> { new() { ImageUrl = p.PrimaryImageUrl, IsPrimary = true } }
                    : new List<ProductImage>(),
                Variants = new List<ProductVariant>
                {
                    new()
                    {
                        Price = p.Price,
                        CompareAtPrice = p.OriginalPrice,
                        Status = WebApplication_ClothingEcommerce.Data.Enums.VariantStatus.Available,
                        Inventory = new Inventory { Quantity = p.TotalStock > 0 ? p.TotalStock : 10, ReservedQuantity = 0 }
                    }
                }
            }).ToList() ?? new List<Product>();

            ViewBag.Categories = categories;
            ViewBag.Brands = brands;

            return View(products);
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
