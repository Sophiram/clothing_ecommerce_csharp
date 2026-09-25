using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Data.Repositories.Interfaces;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public ProductService(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<(List<Product> Products, ProductAdminStatsDto Stats)> GetAdminProductsAsync(
            Guid? categoryId,
            Guid? brandId,
            string? search,
            ProductStatus? status)
        {
            var (products, total, active, outOfStock, inactive) = await _unitOfWork.Products.GetFilteredAdminProductsAsync(
                categoryId, brandId, search, status);

            var stats = new ProductAdminStatsDto
            {
                TotalProducts = total,
                ActiveProducts = active,
                OutOfStockProducts = outOfStock,
                InactiveProducts = inactive
            };

            return (products.ToList(), stats);
        }

        public async Task<Product?> GetProductByIdAsync(Guid id)
        {
            return await _unitOfWork.Products.GetByIdAsync(id);
        }

        public async Task<Product?> GetProductDetailsAsync(Guid id)
        {
            return await _unitOfWork.Products.GetProductWithDetailsAsync(id);
        }

        public async Task<ServiceResult> CreateProductAsync(Product product, string? userId = null, string? userEmail = null, string? ip = null)
        {
            product.Id = Guid.NewGuid();
            product.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

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
            var existing = await _unitOfWork.Products.GetByIdAsync(id);
            if (existing == null) return ServiceResult.Fail("Product not found.");

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.CategoryId = product.CategoryId;
            existing.BrandId = product.BrandId;
            existing.Gender = product.Gender;
            existing.Material = product.Material;
            existing.Status = product.Status;

            _unitOfWork.Products.Update(existing);
            await _unitOfWork.SaveChangesAsync();

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
            var product = await _unitOfWork.Products.GetProductWithDetailsAsync(id);
            if (product == null) return ServiceResult.Fail("Product not found.");

            var variantIds = product.Variants.Select(v => v.Id).ToList();
            var hasOrders = (await _unitOfWork.Repository<OrderItem>().FindAsync(oi => variantIds.Contains(oi.VariantId))).Any();
            if (hasOrders)
            {
                return ServiceResult.Fail("Cannot delete this product because it has associated customer orders. Change status to Inactive instead.");
            }

            var name = product.Name;
            _unitOfWork.Products.Remove(product);
            await _unitOfWork.SaveChangesAsync();

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
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null) return ServiceResult.Fail("Product not found.");

            product.Status = product.Status == ProductStatus.Active ? ProductStatus.Inactive : ProductStatus.Active;
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

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
            var original = await _unitOfWork.Products.GetProductWithDetailsAsync(id);
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

            await _unitOfWork.Products.AddAsync(newProduct);
            await _unitOfWork.SaveChangesAsync();

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
            var categories = (await _unitOfWork.Categories.GetAllAsync()).OrderBy(c => c.Name).ToList();
            var brands = (await _unitOfWork.Brands.GetAllAsync()).OrderBy(b => b.Name).ToList();
            return (categories, brands);
        }
    }
}
