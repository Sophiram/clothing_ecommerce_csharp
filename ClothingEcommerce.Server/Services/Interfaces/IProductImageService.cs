using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public interface IProductImageService
    {
        Task<ServiceResult> AddImageAsync(ProductImage image);
        Task<ServiceResult> UpdateImageAsync(Guid id, Guid productId, string imageUrl, bool isPrimary);
        Task<ServiceResult> SetPrimaryImageAsync(Guid id, Guid productId);
        Task<ServiceResult> DeleteImageAsync(Guid id, Guid productId);
    }
}
