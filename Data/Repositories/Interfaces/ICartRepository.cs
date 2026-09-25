using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Interfaces
{
    public interface ICartRepository : IRepository<Cart>
    {
        Task<Cart?> GetCartByCustomerIdAsync(Guid customerId, bool includeDetails = true);
        Task<Cart?> GetCartByIdWithDetailsAsync(Guid cartId);
        Task<CartItem?> GetCartItemByIdWithDetailsAsync(Guid cartItemId);
        Task<IReadOnlyList<CartItem>> GetAllCartItemsWithDetailsAsync();
        Task<CartItem?> GetFirstCartItemWithDetailsAsync();
        Task<int> GetCartItemCountAsync(Guid customerId);
        Task AddCartItemAsync(CartItem item);
        void UpdateCartItem(CartItem item);
        void RemoveCartItem(CartItem item);
        void RemoveCartItems(IEnumerable<CartItem> items);
    }
}
