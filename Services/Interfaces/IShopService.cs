using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Services
{
    public class ShopFilterParameters
    {
        public Guid? CategoryId { get; set; }
        public Guid? BrandId { get; set; }
        public Guid? SizeId { get; set; }
        public Guid? ColorId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? InStock { get; set; }
        public bool? OnSale { get; set; }
        public string? Search { get; set; }
        public string? Sort { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public string ViewMode { get; set; } = "grid";
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
    }

    public class ProductDetailsResult
    {
        public Product? Product { get; set; }
        public List<Product> RelatedProducts { get; set; } = new();
    }

    public interface IShopService
    {
        Task<(ShopViewModel Model, HashSet<Guid>? WishlistedVariantIds)> GetShopViewModelAsync(ShopFilterParameters filter);
        Task<ProductDetailsResult> GetProductDetailsAsync(Guid id);
        Task<(List<Product> Products, List<Category> Categories, List<Brand> Brands)> GetAdminShopDataAsync(Guid? categoryId, Guid? brandId, string? search);
        Task<Product?> GetAdminProductDetailsAsync(Guid id);
    }
}
