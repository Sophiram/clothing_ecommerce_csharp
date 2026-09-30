using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services.Interfaces
{
    public interface IStoreSettingsService
    {
        Task<StoreReceiptSettings> GetSettingsAsync();
        Task<bool> UpdateStoreProfileAsync(StoreReceiptSettings model, string updatedBy);
        Task<bool> UpdateReceiptCustomizerAsync(StoreReceiptSettings model, string updatedBy);
    }
}
