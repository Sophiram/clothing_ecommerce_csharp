using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Services
{
    public class CartService : ICartService
    {
        private readonly AppDbContext _context;
        private const decimal DeliveryFee = 3.00m;

        public CartService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Customer?> GetCustomerByUserIdOrEmailAsync(string? userId, string? email)
        {
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(userId))
                return null;

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    (!string.IsNullOrEmpty(userId) && c.ApplicationUserId == userId) ||
                    (!string.IsNullOrEmpty(email) && c.Email == email));

            return customer;
        }

        public async Task<CartViewModel> GetCartAsync(Guid customerId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Brand)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Size)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Color)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Inventory)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var subtotal = cart.Items
                .Where(i => i.Variant != null)
                .Sum(i => i.Quantity * i.Variant!.Price);

            var deliveryFee = subtotal > 0 ? DeliveryFee : 0m;

            return new CartViewModel
            {
                Cart = cart,
                SubTotal = subtotal,
                DeliveryFee = deliveryFee,
                Total = subtotal + deliveryFee
            };
        }

        public async Task<CartResult> AddItemAsync(Guid customerId, Guid variantId, int quantity)
        {
            if (quantity < 1) quantity = 1;

            var variant = await _context.ProductVariants
                .Include(v => v.Inventory)
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == variantId);

            if (variant == null)
            {
                return new CartResult { Success = false, Message = "Product variant was not found." };
            }

            var available = variant.Inventory?.AvailableQuantity ?? 0;
            if (available <= 0)
            {
                return new CartResult { Success = false, Message = "This product is currently out of stock." };
            }

            if (quantity > available)
            {
                return new CartResult { Success = false, Message = $"Only {available} item(s) available in stock." };
            }

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var item = cart.Items.FirstOrDefault(i => i.VariantId == variantId);
            if (item == null)
            {
                item = new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    VariantId = variantId,
                    Quantity = quantity
                };
                _context.CartItems.Add(item);
            }
            else
            {
                var newQuantity = item.Quantity + quantity;
                if (newQuantity > available)
                {
                    return new CartResult
                    {
                        Success = false,
                        Message = $"Cannot add more. You have {item.Quantity} in cart and only {available} available in stock."
                    };
                }
                item.Quantity = newQuantity;
            }

            await _context.SaveChangesAsync();

            var totalCount = await GetCartCountAsync(customerId);
            return new CartResult
            {
                Success = true,
                Message = $"'{variant.Product?.Name ?? "Item"}' added to your shopping bag.",
                CartCount = totalCount
            };
        }

        public async Task<CartResult> UpdateQuantityAsync(Guid customerId, Guid cartItemId, int quantity)
        {
            var item = await _context.CartItems
                .Include(i => i.Cart)
                .Include(i => i.Variant)
                    .ThenInclude(v => v.Inventory)
                .FirstOrDefaultAsync(i => i.Id == cartItemId && i.Cart.CustomerId == customerId);

            if (item == null)
            {
                return new CartResult { Success = false, Message = "Item not found in your cart." };
            }

            if (quantity <= 0)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
                var count = await GetCartCountAsync(customerId);
                return new CartResult { Success = true, Message = "Item removed from cart.", CartCount = count };
            }

            var available = item.Variant?.Inventory?.AvailableQuantity ?? 0;
            if (available <= 0)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
                return new CartResult { Success = false, Message = "This product is out of stock and was removed from your cart." };
            }

            if (quantity > available)
            {
                item.Quantity = available;
                await _context.SaveChangesAsync();
                return new CartResult { Success = false, Message = $"Only {available} item(s) available in stock. Quantity adjusted." };
            }

            item.Quantity = quantity;
            await _context.SaveChangesAsync();

            var newCount = await GetCartCountAsync(customerId);
            return new CartResult { Success = true, Message = "Cart updated successfully.", CartCount = newCount };
        }

        public async Task<CartResult> RemoveItemAsync(Guid customerId, Guid cartItemId)
        {
            var item = await _context.CartItems
                .Include(i => i.Cart)
                .FirstOrDefaultAsync(i => i.Id == cartItemId && i.Cart.CustomerId == customerId);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            var count = await GetCartCountAsync(customerId);
            return new CartResult { Success = true, Message = "Item removed from cart.", CartCount = count };
        }

        public async Task<int> GetCartCountAsync(Guid customerId)
        {
            return await _context.CartItems
                .Where(ci => ci.Cart.CustomerId == customerId)
                .SumAsync(ci => (int?)ci.Quantity) ?? 0;
        }

        public async Task ClearCartAsync(Guid customerId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart != null && cart.Items.Any())
            {
                _context.CartItems.RemoveRange(cart.Items);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Cart?> GetAdminCustomerCartAsync(Guid customerId)
        {
            return await _context.Carts
                .Include(c => c.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Product)
                .Include(c => c.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Color)
                .Include(c => c.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Size)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);
        }

        public async Task<List<CartItem>> GetAllCartItemsAsync()
        {
            return await _context.CartItems.AsNoTracking().ToListAsync();
        }

        public async Task<CartItem?> GetCartItemByIdAsync(Guid id)
        {
            return await _context.CartItems.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<ServiceResult> CreateCartItemAsync(CartItem item)
        {
            if (item.Id == Guid.Empty)
            {
                item.Id = Guid.NewGuid();
            }
            _context.CartItems.Add(item);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Cart item created successfully.");
        }

        public async Task<ServiceResult> UpdateCartItemAsync(Guid id, CartItem item)
        {
            var existing = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == id);
            if (existing == null)
            {
                return ServiceResult.Fail("Cart item not found.");
            }

            existing.CartId = item.CartId;
            existing.VariantId = item.VariantId;
            existing.Quantity = item.Quantity;

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Cart item updated successfully.");
        }

        public async Task<ServiceResult> DeleteCartItemAsync(Guid id)
        {
            var item = await _context.CartItems.FindAsync(id);
            if (item == null)
            {
                return ServiceResult.Fail("Cart item not found.");
            }

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Cart item deleted successfully.");
        }
    }
}
