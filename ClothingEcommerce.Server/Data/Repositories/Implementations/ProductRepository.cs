using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Data.Repositories.Interfaces;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Implementations
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Product?> GetProductWithDetailsAsync(Guid productId)
        {
            return await _dbSet
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Color)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Size)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Inventory)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.Customer)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);
        }

        public async Task<IReadOnlyList<Product>> GetProductsWithDetailsAsync()
        {
            return await _dbSet
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .OrderByDescending(p => p.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Product>> GetFeaturedProductsAsync(int take = 8)
        {
            return await _dbSet
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Where(p => p.Status == ProductStatus.Active)
                .OrderByDescending(p => p.CreatedAt)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Product>> GetSaleProductsAsync(int take = 8)
        {
            return await _dbSet
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Where(p => p.Variants.Any(v => v.CompareAtPrice.HasValue && v.CompareAtPrice > v.Price))
                .OrderByDescending(p => p.CreatedAt)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ProductVariant?> GetVariantByIdWithDetailsAsync(Guid variantId)
        {
            return await _context.ProductVariants
                .Include(v => v.Product)
                    .ThenInclude(p => p.Images)
                .Include(v => v.Product)
                    .ThenInclude(p => p.Brand)
                .Include(v => v.Color)
                .Include(v => v.Size)
                .Include(v => v.Inventory)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == variantId);
        }

        public async Task<IReadOnlyList<ProductVariant>> GetVariantsByProductIdAsync(Guid productId)
        {
            return await _context.ProductVariants
                .Include(v => v.Color)
                .Include(v => v.Size)
                .Include(v => v.Inventory)
                .Where(v => v.ProductId == productId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(IReadOnlyList<Product> Products, int TotalCount, int ActiveCount, int OutOfStockCount, int InactiveCount)> GetFilteredAdminProductsAsync(
            Guid? categoryId,
            Guid? brandId,
            string? search,
            ProductStatus? status)
        {
            IQueryable<Product> query = _dbSet
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Variants);

            if (categoryId.HasValue && categoryId.Value != Guid.Empty)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (brandId.HasValue && brandId.Value != Guid.Empty)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(p => p.Name.Contains(s) || (p.Description != null && p.Description.Contains(s)));
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            var all = await _dbSet.AsNoTracking().ToListAsync();

            return (
                products,
                all.Count,
                all.Count(p => p.Status == ProductStatus.Active),
                all.Count(p => p.Status == ProductStatus.OutOfStock),
                all.Count(p => p.Status == ProductStatus.Inactive)
            );
        }
    }
}
