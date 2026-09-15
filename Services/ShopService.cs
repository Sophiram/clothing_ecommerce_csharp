using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Services
{
    public class ShopService : IShopService
    {
        private readonly AppDbContext _context;

        public ShopService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(ShopViewModel Model, HashSet<Guid>? WishlistedVariantIds)> GetShopViewModelAsync(ShopFilterParameters filter)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Inventory)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Size)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Color)
                .Where(p => p.Status == ProductStatus.Active)
                .AsQueryable();

            if (filter.CategoryId.HasValue && filter.CategoryId.Value != Guid.Empty)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            if (filter.BrandId.HasValue && filter.BrandId.Value != Guid.Empty)
            {
                query = query.Where(p => p.BrandId == filter.BrandId.Value);
            }

            if (filter.SizeId.HasValue && filter.SizeId.Value != Guid.Empty)
            {
                query = query.Where(p => p.Variants.Any(v => v.SizeId == filter.SizeId.Value && v.Status == VariantStatus.Available));
            }

            if (filter.ColorId.HasValue && filter.ColorId.Value != Guid.Empty)
            {
                query = query.Where(p => p.Variants.Any(v => v.ColorId == filter.ColorId.Value && v.Status == VariantStatus.Available));
            }

            if (filter.MinPrice.HasValue && filter.MinPrice.Value > 0)
            {
                query = query.Where(p => p.Variants.Any(v => v.Price >= filter.MinPrice.Value));
            }

            if (filter.MaxPrice.HasValue && filter.MaxPrice.Value > 0)
            {
                query = query.Where(p => p.Variants.Any(v => v.Price <= filter.MaxPrice.Value));
            }

            if (filter.InStock == true)
            {
                query = query.Where(p => p.Variants.Any(v =>
                    v.Status == VariantStatus.Available &&
                    v.Inventory != null &&
                    v.Inventory.AvailableQuantity > 0));
            }

            if (filter.OnSale == true)
            {
                query = query.Where(p => p.Variants.Any(v =>
                    v.CompareAtPrice != null &&
                    v.CompareAtPrice > v.Price));
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                query = query.Where(p =>
                    p.Name.Contains(s) ||
                    (p.Description != null && p.Description.Contains(s)) ||
                    (p.Brand != null && p.Brand.Name.Contains(s)) ||
                    (p.Category != null && p.Category.Name.Contains(s)));
            }

            query = filter.Sort switch
            {
                "price-low" => query.OrderBy(p => p.Variants.Select(v => (decimal?)v.Price).Min() ?? 0),
                "price-high" => query.OrderByDescending(p => p.Variants.Select(v => (decimal?)v.Price).Max() ?? 0),
                "name" => query.OrderBy(p => p.Name),
                "name-desc" => query.OrderByDescending(p => p.Name),
                "rating" => query.OrderByDescending(p => p.Reviews.Select(r => (double?)r.Rating).Average() ?? 0),
                "oldest" => query.OrderBy(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var pageSize = filter.PageSize <= 0 || filter.PageSize > 48 ? 12 : filter.PageSize;
            var totalProducts = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            var page = filter.Page < 1 ? 1 : filter.Page > totalPages ? totalPages : filter.Page;

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            var brands = await _context.Brands
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .ToListAsync();

            var sizes = await _context.Sizes
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync();

            var colors = await _context.Colors
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            var categoryCounts = await _context.Products
                .Where(p => p.Status == ProductStatus.Active)
                .GroupBy(p => p.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

            var brandCounts = await _context.Products
                .Where(p => p.Status == ProductStatus.Active)
                .GroupBy(p => p.BrandId)
                .Select(g => new { BrandId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BrandId, x => x.Count);

            var allVariants = _context.ProductVariants.Where(v => v.Status == VariantStatus.Available);
            decimal minBound = 0;
            decimal maxBound = 500;
            if (await allVariants.AnyAsync())
            {
                minBound = Math.Floor(await allVariants.MinAsync(v => v.Price));
                maxBound = Math.Ceiling(await allVariants.MaxAsync(v => v.Price));
                if (maxBound <= minBound) maxBound = minBound + 100;
            }

            HashSet<Guid>? wishlistedVariantIds = null;
            if (!string.IsNullOrEmpty(filter.UserId))
            {
                var customer = await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ApplicationUserId == filter.UserId || c.Email == filter.UserEmail);

                if (customer != null)
                {
                    var ids = await _context.WishlistItems
                        .Where(w => w.Wishlist.CustomerId == customer.Id)
                        .Select(w => w.VariantId)
                        .ToListAsync();

                    wishlistedVariantIds = new HashSet<Guid>(ids);
                }
            }

            var model = new ShopViewModel
            {
                Products = products,
                Categories = categories,
                Brands = brands,
                Sizes = sizes,
                Colors = colors,
                CategoryCounts = categoryCounts,
                BrandCounts = brandCounts,
                CategoryId = filter.CategoryId,
                BrandId = filter.BrandId,
                SizeId = filter.SizeId,
                ColorId = filter.ColorId,
                MinPrice = filter.MinPrice,
                MaxPrice = filter.MaxPrice,
                InStock = filter.InStock,
                OnSale = filter.OnSale,
                Search = filter.Search,
                Sort = filter.Sort,
                PriceMinBound = minBound,
                PriceMaxBound = maxBound,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalProducts = totalProducts,
                ViewMode = filter.ViewMode == "list" ? "list" : "grid"
            };

            return (model, wishlistedVariantIds);
        }

        public async Task<ProductDetailsResult> GetProductDetailsAsync(Guid id)
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
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.Customer)
                .FirstOrDefaultAsync(p => p.Id == id && p.Status == ProductStatus.Active);

            if (product == null)
            {
                return new ProductDetailsResult { Product = null };
            }

            var related = await _context.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Inventory)
                .Include(p => p.Reviews)
                .Where(p => p.Id != product.Id &&
                            p.Status == ProductStatus.Active &&
                            (p.CategoryId == product.CategoryId || p.BrandId == product.BrandId))
                .Take(4)
                .ToListAsync();

            return new ProductDetailsResult
            {
                Product = product,
                RelatedProducts = related
            };
        }

        public async Task<(List<Product> Products, List<Category> Categories, List<Brand> Brands)> GetAdminShopDataAsync(Guid? categoryId, Guid? brandId, string? search)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (brandId.HasValue)
                query = query.Where(p => p.BrandId == brandId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(p => p.Name.Contains(s) || (p.Description != null && p.Description.Contains(s)));
            }

            var categories = await _context.Categories.AsNoTracking().ToListAsync();
            var brands = await _context.Brands.AsNoTracking().ToListAsync();
            var products = await query.AsNoTracking().ToListAsync();

            return (products, categories, brands);
        }

        public async Task<Product?> GetAdminProductDetailsAsync(Guid id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
