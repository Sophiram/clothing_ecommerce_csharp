using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Interfaces
{
    public interface IWishlistRepository : IRepository<Wishlist>
    {
        Task<Wishlist?> GetWishlistByCustomerIdAsync(Guid customerId, bool includeDetails = true);
        Task<bool> IsVariantWishlistedAsync(Guid customerId, Guid variantId);
        Task<IReadOnlyList<Guid>> GetWishlistedVariantIdsAsync(Guid customerId);
        Task AddWishlistItemAsync(WishlistItem item);
        void RemoveWishlistItem(WishlistItem item);
    }
}
