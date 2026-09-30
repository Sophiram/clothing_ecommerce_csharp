using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public interface IWishlistService
    {
        Task<List<Wishlist>> GetAllWishlistsAsync(string? search = null);
        Task<Wishlist?> GetWishlistByIdAsync(Guid id);
        Task<Wishlist> GetOrCreateCustomerWishlistAsync(Guid customerId);
        Task<(bool Success, bool IsWishlisted, string Message)> ToggleWishlistAsync(Guid customerId, Guid variantId);
        Task<(bool Success, string Message)> AddToWishlistAsync(Guid customerId, Guid variantId);
        Task<(bool Success, string Message)> RemoveFromWishlistAsync(Guid customerId, Guid? id, Guid? variantId);
        Task<(bool Success, string Message)> MoveToCartAsync(Guid customerId, Guid wishlistItemId);
        Task<bool> ClearWishlistAsync(Guid customerId);
        Task<bool> DeleteWishlistAsync(Guid id);

        // WishlistItem Admin CRUD
        Task<List<WishlistItem>> GetAllWishlistItemsAsync();
        Task<WishlistItem?> GetWishlistItemByIdAsync(Guid id);
        Task<ServiceResult> CreateWishlistItemAsync(WishlistItem item);
        Task<ServiceResult> UpdateWishlistItemAsync(Guid id, WishlistItem item);
        Task<ServiceResult> DeleteWishlistItemAsync(Guid id);
        Task<bool> WishlistItemExistsAsync(Guid id);
    }
}
