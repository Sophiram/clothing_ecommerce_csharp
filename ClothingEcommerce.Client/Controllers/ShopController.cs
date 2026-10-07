using ClothingEcommerce.Client.Services.ApiClient;
using ClothingEcommerce.Shared.DTOs.Catalog;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace ClothingEcommerce.Client.Controllers
{
    public class ShopController : Controller
    {
        private readonly IApiClient _apiClient;

        public ShopController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // GET: /Shop
        [HttpGet]
        public async Task<IActionResult> Index(
            Guid? categoryId,
            Guid? brandId,
            Guid? sizeId,
            Guid? colorId,
            decimal? minPrice,
            decimal? maxPrice,
            bool? inStock,
            bool? onSale,
            string? search,
            string? sort = "newest",
            int page = 1,
            int pageSize = 12,
            string viewMode = "grid")
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(search)) queryParams.Add($"searchTerm={Uri.EscapeDataString(search.Trim())}");
            if (categoryId.HasValue && categoryId != Guid.Empty) queryParams.Add($"categoryId={categoryId.Value}");
            if (brandId.HasValue && brandId != Guid.Empty) queryParams.Add($"brandId={brandId.Value}");
            if (sizeId.HasValue && sizeId != Guid.Empty) queryParams.Add($"sizeId={sizeId.Value}");
            if (colorId.HasValue && colorId != Guid.Empty) queryParams.Add($"colorId={colorId.Value}");
            if (minPrice.HasValue) queryParams.Add($"minPrice={minPrice.Value}");
            if (maxPrice.HasValue) queryParams.Add($"maxPrice={maxPrice.Value}");
            if (inStock == true) queryParams.Add("inStockOnly=true");
            if (onSale == true) queryParams.Add("onSaleOnly=true");
            if (!string.IsNullOrWhiteSpace(sort)) queryParams.Add($"sortBy={Uri.EscapeDataString(sort)}");
            queryParams.Add($"page={page}");
            queryParams.Add($"pageSize={pageSize}");

            var queryString = string.Join("&", queryParams);
            var endpoint = $"api/products?{queryString}";

            var productsTask = _apiClient.GetAsync<PagedResult<ProductDto>>(endpoint);
            var categoriesTask = _apiClient.GetAsync<List<CategoryDto>>("api/categories");
            var brandsTask = _apiClient.GetAsync<List<BrandDto>>("api/brands");
            var sizesTask = _apiClient.GetAsync<List<SizeDto>>("api/sizes");
            var colorsTask = _apiClient.GetAsync<List<ColorDto>>("api/colors");

            await Task.WhenAll(productsTask, categoriesTask, brandsTask, sizesTask, colorsTask);

            var productsResponse = await productsTask;
            var categoriesResponse = await categoriesTask;
            var brandsResponse = await brandsTask;
            var sizesResponse = await sizesTask;
            var colorsResponse = await colorsTask;

            var viewModel = new ShopViewModel
            {
                Products = productsResponse?.Data?.Items?.Select(p => new Product
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    CategoryId = p.CategoryId ?? Guid.Empty,
                    Category = new Category { Id = p.CategoryId ?? Guid.Empty, Name = p.CategoryName ?? "" },
                    BrandId = p.BrandId ?? Guid.Empty,
                    Brand = new Brand { Id = p.BrandId ?? Guid.Empty, Name = p.BrandName ?? "" },
                    Images = !string.IsNullOrEmpty(p.PrimaryImageUrl)
                        ? new List<ProductImage> { new() { ImageUrl = p.PrimaryImageUrl, IsPrimary = true } }
                        : new List<ProductImage>(),
                    Variants = new List<ProductVariant>
                    {
                        new()
                        {
                            Id = p.DefaultVariantId ?? Guid.Empty,
                            ProductId = p.Id,
                            Price = p.Price,
                            CompareAtPrice = p.OriginalPrice,
                            Status = WebApplication_ClothingEcommerce.Data.Enums.VariantStatus.Available,
                            Inventory = new Inventory { Quantity = p.TotalStock > 0 ? p.TotalStock : 10, ReservedQuantity = 0 }
                        }
                    }
                }).ToList() ?? new List<Product>(),

                Categories = categoriesResponse?.Data?.Select(c => new Category
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description
                }).ToList() ?? new List<Category>(),

                Brands = brandsResponse?.Data?.Select(b => new Brand
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description
                }).ToList() ?? new List<Brand>(),

                Sizes = sizesResponse?.Data?.Select(s => new Size
                {
                    Id = s.Id,
                    Name = s.Name
                }).ToList() ?? new List<Size>(),

                Colors = colorsResponse?.Data?.Select(c => new Color
                {
                    Id = c.Id,
                    Name = c.Name,
                    HexCode = c.HexCode
                }).ToList() ?? new List<Color>(),

                CategoryCounts = categoriesResponse?.Data?.ToDictionary(c => c.Id, c => c.ProductCount) ?? new(),
                BrandCounts = brandsResponse?.Data?.ToDictionary(b => b.Id, b => b.ProductCount) ?? new(),

                CategoryId = categoryId,
                BrandId = brandId,
                SizeId = sizeId,
                ColorId = colorId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                InStock = inStock,
                OnSale = onSale,
                Search = search,
                Sort = sort ?? "newest",

                CurrentPage = productsResponse?.Data?.PageNumber ?? 1,
                TotalPages = productsResponse?.Data?.TotalPages ?? 1,
                PageSize = productsResponse?.Data?.PageSize ?? 12,
                TotalProducts = productsResponse?.Data?.TotalCount ?? 0,
                ViewMode = viewMode
            };

            return View(viewModel);
        }

        // GET: /Shop/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var response = await _apiClient.GetAsync<ProductDetailDto>($"api/products/{id}");
            if (response == null || !response.Success || response.Data == null)
            {
                return NotFound();
            }

            var dto = response.Data;
            var product = new Product
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                CategoryId = dto.CategoryId ?? Guid.Empty,
                Category = new Category { Id = dto.CategoryId ?? Guid.Empty, Name = dto.CategoryName ?? "" },
                BrandId = dto.BrandId ?? Guid.Empty,
                Brand = new Brand { Id = dto.BrandId ?? Guid.Empty, Name = dto.BrandName ?? "" },
                Images = dto.Images?.Select(img => new ProductImage
                {
                    Id = img.Id,
                    ImageUrl = img.ImageUrl,
                    IsPrimary = img.IsPrimary
                }).ToList() ?? new List<ProductImage>(),
                Variants = dto.Variants?.Select(v => new ProductVariant
                {
                    Id = v.Id,
                    ProductId = v.ProductId,
                    SizeId = v.SizeId ?? Guid.Empty,
                    Size = v.SizeId.HasValue ? new Size { Id = v.SizeId.Value, Name = v.SizeName ?? "" } : null,
                    ColorId = v.ColorId ?? Guid.Empty,
                    Color = v.ColorId.HasValue ? new Color { Id = v.ColorId.Value, Name = v.ColorName ?? "", HexCode = v.ColorHex ?? "" } : null,
                    SKU = v.Sku,
                    Price = v.Price,
                    CompareAtPrice = dto.OriginalPrice,
                    Status = v.IsActive ? WebApplication_ClothingEcommerce.Data.Enums.VariantStatus.Available : WebApplication_ClothingEcommerce.Data.Enums.VariantStatus.Discontinued,
                    Inventory = new Inventory { Quantity = v.StockQuantity, ReservedQuantity = 0 }
                }).ToList() ?? new List<ProductVariant>()
            };

            return View(product);
        }
    }
}
