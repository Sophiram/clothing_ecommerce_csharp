using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;
        private readonly IAuditService _auditService;

        public ProductService(AppDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<(List<Product> Products, ProductAdminStatsDto Stats)> GetAdminProductsAsync(
            Guid? categoryId,
            Guid? brandId,
            string? search,
            ProductStatus? status)
        {
            IQueryable<Product> query = _context.Products
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

            var all = await _context.Products.AsNoTracking().ToListAsync();
            var stats = new ProductAdminStatsDto
            {
                TotalProducts = all.Count,
                ActiveProducts = all.Count(p => p.Status == ProductStatus.Active),
                OutOfStockProducts = all.Count(p => p.Status == ProductStatus.OutOfStock),
                InactiveProducts = all.Count(p => p.Status == ProductStatus.Inactive)
            };

            return (products, stats);
        }

        public async Task<Product?> GetProductByIdAsync(Guid id)
        {
            return await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product?> GetProductDetailsAsync(Guid id)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Variants).ThenInclude(v => v.Size)
                .Include(p => p.Variants).ThenInclude(v => v.Color)
                .Include(p => p.Variants).ThenInclude(v => v.Inventory)
                .Include(p => p.Reviews).ThenInclude(r => r.Customer)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<ServiceResult> CreateProductAsync(Product product, string? userId = null, string? userEmail = null, string? ip = null)
        {
            product.Id = Guid.NewGuid();
            product.CreatedAt = DateTime.UtcNow;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                userEmail,
                "CreateProduct",
                "Product",
                product.Id.ToString(),
                $"Created product '{product.Name}'",
                ip);

            return ServiceResult.Ok("Product created successfully.");
        }

        public async Task<ServiceResult> UpdateProductAsync(Guid id, Product product, string? userId = null, string? userEmail = null, string? ip = null)
        {
            var existing = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null) return ServiceResult.Fail("Product not found.");

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.CategoryId = product.CategoryId;
            existing.BrandId = product.BrandId;
            existing.Status = product.Status;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                userEmail,
                "UpdateProduct",
                "Product",
                id.ToString(),
                $"Updated product '{existing.Name}'",
                ip);

            return ServiceResult.Ok("Product updated successfully.");
        }

        public async Task<ServiceResult> DeleteProductAsync(Guid id, string? userId = null, string? userEmail = null, string? ip = null)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return ServiceResult.Fail("Product not found.");

            var variantIds = product.Variants.Select(v => v.Id).ToList();
            var hasOrders = await _context.OrderItems.AnyAsync(oi => variantIds.Contains(oi.VariantId));
            if (hasOrders)
            {
                return ServiceResult.Fail("Cannot delete this product because it has associated customer orders. Change status to Inactive instead.");
            }

            var name = product.Name;
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                userEmail,
                "DeleteProduct",
                "Product",
                id.ToString(),
                $"Deleted product '{name}'",
                ip);

            return ServiceResult.Ok($"Product '{name}' was deleted successfully.");
        }

        public async Task<ServiceResult> ToggleStatusAsync(Guid id, string? userId = null, string? userEmail = null, string? ip = null)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return ServiceResult.Fail("Product not found.");

            product.Status = product.Status == ProductStatus.Active ? ProductStatus.Inactive : ProductStatus.Active;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                userEmail,
                "ToggleProductStatus",
                "Product",
                id.ToString(),
                $"Toggled product '{product.Name}' status to {product.Status}",
                ip);

            return ServiceResult.Ok($"Product '{product.Name}' is now {product.Status}.");
        }

        public async Task<(ServiceResult Result, Guid? NewProductId)> DuplicateProductAsync(Guid id, string? userId = null, string? userEmail = null, string? ip = null)
        {
            var original = await _context.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (original == null) return (ServiceResult.Fail("Original product not found."), null);

            var newProduct = new Product
            {
                Id = Guid.NewGuid(),
                Name = $"{original.Name} (Copy)",
                Description = original.Description,
                CategoryId = original.CategoryId,
                BrandId = original.BrandId,
                Status = ProductStatus.Inactive,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var img in original.Images)
            {
                newProduct.Images.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = newProduct.Id,
                    ImageUrl = img.ImageUrl,
                    IsPrimary = img.IsPrimary
                });
            }

            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                userEmail,
                "DuplicateProduct",
                "Product",
                newProduct.Id.ToString(),
                $"Duplicated product '{original.Name}' as '{newProduct.Name}'",
                ip);

            return (ServiceResult.Ok($"Product duplicated as '{newProduct.Name}'."), newProduct.Id);
        }

        public async Task<(List<Category> Categories, List<Brand> Brands)> GetCategoriesAndBrandsAsync()
        {
            var categories = await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            var brands = await _context.Brands.AsNoTracking().OrderBy(b => b.Name).ToListAsync();
            return (categories, brands);
        }
    }
}
