using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class ProductAdminStatsDto
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int InactiveProducts { get; set; }
    }

    public interface IProductService
    {
        Task<(List<Product> Products, ProductAdminStatsDto Stats)> GetAdminProductsAsync(Guid? categoryId, Guid? brandId, string? search, ProductStatus? status);
        Task<Product?> GetProductByIdAsync(Guid id);
        Task<Product?> GetProductDetailsAsync(Guid id);
        Task<ServiceResult> CreateProductAsync(Product product, string? userId = null, string? userEmail = null, string? ip = null);
        Task<ServiceResult> UpdateProductAsync(Guid id, Product product, string? userId = null, string? userEmail = null, string? ip = null);
        Task<ServiceResult> DeleteProductAsync(Guid id, string? userId = null, string? userEmail = null, string? ip = null);
        Task<ServiceResult> ToggleStatusAsync(Guid id, string? userId = null, string? userEmail = null, string? ip = null);
        Task<(ServiceResult Result, Guid? NewProductId)> DuplicateProductAsync(Guid id, string? userId = null, string? userEmail = null, string? ip = null);
        Task<(List<Category> Categories, List<Brand> Brands)> GetCategoriesAndBrandsAsync();
    }
}
