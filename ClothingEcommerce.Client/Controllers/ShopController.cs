using ClothingEcommerce.Client.Services.ApiClient;
using ClothingEcommerce.Shared.DTOs.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace ClothingEcommerce.Client.Controllers
{
    public class ShopController : Controller
    {
        private readonly IApiClient _apiClient;

        public ShopController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index([FromQuery] ProductFilterDto filter)
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm)) queryParams.Add($"searchTerm={Uri.EscapeDataString(filter.SearchTerm)}");
            if (filter.CategoryId.HasValue) queryParams.Add($"categoryId={filter.CategoryId}");
            if (filter.BrandId.HasValue) queryParams.Add($"brandId={filter.BrandId}");
            if (filter.MinPrice.HasValue) queryParams.Add($"minPrice={filter.MinPrice}");
            if (filter.MaxPrice.HasValue) queryParams.Add($"maxPrice={filter.MaxPrice}");
            if (!string.IsNullOrWhiteSpace(filter.SortBy)) queryParams.Add($"sortBy={filter.SortBy}");
            queryParams.Add($"page={filter.Page}");
            queryParams.Add($"pageSize={filter.PageSize}");

            var queryString = string.Join("&", queryParams);
            var endpoint = $"api/products?{queryString}";

            var productsResponse = await _apiClient.GetAsync<PagedResult<ProductDto>>(endpoint);
            var categoriesResponse = await _apiClient.GetAsync<List<CategoryDto>>("api/categories");
            var brandsResponse = await _apiClient.GetAsync<List<BrandDto>>("api/brands");

            ViewBag.Categories = categoriesResponse?.Data ?? new List<CategoryDto>();
            ViewBag.Brands = brandsResponse?.Data ?? new List<BrandDto>();
            ViewBag.Filter = filter;

            return View(productsResponse?.Data ?? new PagedResult<ProductDto>());
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var response = await _apiClient.GetAsync<ProductDetailDto>($"api/products/{id}");
            if (response == null || !response.Success || response.Data == null)
            {
                return NotFound();
            }

            return View(response.Data);
        }
    }
}
