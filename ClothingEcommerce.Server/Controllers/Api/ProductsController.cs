using ClothingEcommerce.Shared.Common;
using ClothingEcommerce.Shared.DTOs.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;

namespace ClothingEcommerce.Server.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] ProductFilterDto filter)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Inventory)
                .Where(p => p.Status == ProductStatus.Active);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) ||
                                         (p.Description != null && p.Description.ToLower().Contains(term)));
            }

            if (filter.CategoryId.HasValue && filter.CategoryId != Guid.Empty)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            if (filter.BrandId.HasValue && filter.BrandId != Guid.Empty)
            {
                query = query.Where(p => p.BrandId == filter.BrandId.Value);
            }

            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.Variants.Any(v => v.Price >= filter.MinPrice.Value));
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Variants.Any(v => v.Price <= filter.MaxPrice.Value));
            }

            // Sorting
            query = filter.SortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Variants.Min(v => v.Price)),
                "price_desc" => query.OrderByDescending(p => p.Variants.Max(v => v.Price)),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 12 : Math.Min(filter.PageSize, 50);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Variants.Select(v => v.Price).FirstOrDefault(),
                    OriginalPrice = p.Variants.Select(v => v.CompareAtPrice).FirstOrDefault(),
                    PrimaryImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault()
                                      ?? p.Images.Select(i => i.ImageUrl).FirstOrDefault(),
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    BrandId = p.BrandId,
                    BrandName = p.Brand != null ? p.Brand.Name : null,
                    TotalStock = p.Variants.Where(v => v.Inventory != null).Sum(v => v.Inventory!.Quantity - v.Inventory!.ReservedQuantity)
                })
                .ToListAsync();

            var result = new PagedResult<ProductDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<ProductDto>>.Ok(result));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Size)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Color)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Inventory)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound(ApiResponse<ProductDetailDto>.Fail("Product not found.", 404));
            }

            var dto = new ProductDetailDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Variants.Select(v => v.Price).FirstOrDefault(),
                OriginalPrice = product.Variants.Select(v => v.CompareAtPrice).FirstOrDefault(),
                PrimaryImageUrl = product.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault()
                                  ?? product.Images.Select(i => i.ImageUrl).FirstOrDefault(),
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                BrandId = product.BrandId,
                BrandName = product.Brand?.Name,
                TotalStock = product.Variants.Where(v => v.Inventory != null).Sum(v => v.Inventory!.Quantity - v.Inventory!.ReservedQuantity),
                Images = product.Images.Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsPrimary = i.IsPrimary
                }).ToList(),
                Variants = product.Variants.Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    ProductId = v.ProductId,
                    SizeId = v.SizeId,
                    SizeName = v.Size?.Name,
                    ColorId = v.ColorId,
                    ColorName = v.Color?.Name,
                    ColorHex = v.Color?.HexCode,
                    Sku = v.SKU,
                    Price = v.Price,
                    StockQuantity = v.Inventory != null ? v.Inventory.Quantity - v.Inventory.ReservedQuantity : 0,
                    IsActive = v.Status == VariantStatus.Available
                }).ToList()
            };

            return Ok(ApiResponse<ProductDetailDto>.Ok(dto));
        }

        [HttpGet("featured")]
        public async Task<IActionResult> GetFeaturedProducts([FromQuery] int count = 8)
        {
            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Inventory)
                .Where(p => p.Status == ProductStatus.Active)
                .Take(count)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Variants.Select(v => v.Price).FirstOrDefault(),
                    OriginalPrice = p.Variants.Select(v => v.CompareAtPrice).FirstOrDefault(),
                    PrimaryImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault()
                                      ?? p.Images.Select(i => i.ImageUrl).FirstOrDefault(),
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    BrandId = p.BrandId,
                    BrandName = p.Brand != null ? p.Brand.Name : null
                })
                .ToListAsync();

            return Ok(ApiResponse<List<ProductDto>>.Ok(products));
        }
    }
}
