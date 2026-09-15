using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public interface IProductVariantService
    {
        Task<List<ProductVariant>> GetVariantsAsync(Guid? productId);
        Task<ProductVariant?> GetVariantDetailsAsync(Guid id);
        Task<(List<Product> Products, List<Size> Sizes, List<Color> Colors)> GetVariantDropdownDataAsync();
        Task<ServiceResult> CreateVariantAsync(ProductVariant variant, int quantity);
        Task<ServiceResult> UpdateVariantAsync(Guid id, ProductVariant variant, int quantity);
        Task<ServiceResult> DeleteVariantAsync(Guid id);
    }
}
