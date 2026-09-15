using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Services
{
    public class CartResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CartCount { get; set; }
    }

    public interface ICartService
    {
        Task<Customer?> GetCustomerByUserIdOrEmailAsync(string? userId, string? email);
        Task<CartViewModel> GetCartAsync(Guid customerId);
        Task<CartResult> AddItemAsync(Guid customerId, Guid variantId, int quantity);
        Task<CartResult> UpdateQuantityAsync(Guid customerId, Guid cartItemId, int quantity);
        Task<CartResult> RemoveItemAsync(Guid customerId, Guid cartItemId);
        Task<int> GetCartCountAsync(Guid customerId);
        Task ClearCartAsync(Guid customerId);

        // Admin operations
        Task<Cart?> GetAdminCustomerCartAsync(Guid customerId);
        Task<List<CartItem>> GetAllCartItemsAsync();
        Task<CartItem?> GetCartItemByIdAsync(Guid id);
        Task<ServiceResult> CreateCartItemAsync(CartItem item);
        Task<ServiceResult> UpdateCartItemAsync(Guid id, CartItem item);
        Task<ServiceResult> DeleteCartItemAsync(Guid id);
    }
}
