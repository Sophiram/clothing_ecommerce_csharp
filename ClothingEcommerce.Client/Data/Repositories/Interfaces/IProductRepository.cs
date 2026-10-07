using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<Product?> GetProductWithDetailsAsync(Guid productId);
        Task<IReadOnlyList<Product>> GetProductsWithDetailsAsync();
        Task<IReadOnlyList<Product>> GetFeaturedProductsAsync(int take = 8);
        Task<IReadOnlyList<Product>> GetSaleProductsAsync(int take = 8);
        Task<ProductVariant?> GetVariantByIdWithDetailsAsync(Guid variantId);
        Task<IReadOnlyList<ProductVariant>> GetVariantsByProductIdAsync(Guid productId);
        Task<(IReadOnlyList<Product> Products, int TotalCount, int ActiveCount, int OutOfStockCount, int InactiveCount)> GetFilteredAdminProductsAsync(
            Guid? categoryId,
            Guid? brandId,
            string? search,
            WebApplication_ClothingEcommerce.Data.Enums.ProductStatus? status);
    }
}
