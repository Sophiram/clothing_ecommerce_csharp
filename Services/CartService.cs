using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Data.Repositories.Interfaces;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Services
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;
        private const decimal DeliveryFee = 3.00m;

        public CartService(IUnitOfWork unitOfWork, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<Customer?> GetCustomerByUserIdOrEmailAsync(string? userId, string? email)
        {
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(userId))
                return null;

            var customer = await _unitOfWork.Customers.GetByUserIdOrEmailAsync(userId, email);

            if (customer == null && !string.IsNullOrWhiteSpace(email))
            {
                var appUser = !string.IsNullOrEmpty(userId) ? await _context.Users.FindAsync(userId) : null;
                customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    ApplicationUserId = userId,
                    FirstName = appUser?.FirstName ?? email.Split('@')[0],
                    LastName = appUser?.LastName ?? "",
                    Email = email,
                    Phone = appUser?.PhoneNumber ?? "",
                    Status = CustomerStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Customers.AddAsync(customer);
                await _unitOfWork.SaveChangesAsync();
            }
            else if (customer != null && string.IsNullOrEmpty(customer.ApplicationUserId) && !string.IsNullOrEmpty(userId))
            {
                customer.ApplicationUserId = userId;
                _unitOfWork.Customers.Update(customer);
                await _unitOfWork.SaveChangesAsync();
            }

            return customer;
        }

        public async Task<CartViewModel> GetCartAsync(Guid customerId)
        {
            var cart = await _unitOfWork.Carts.GetCartByCustomerIdAsync(customerId, includeDetails: true);

            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Carts.AddAsync(cart);
                await _unitOfWork.SaveChangesAsync();
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

            var variant = await _unitOfWork.Products.GetVariantByIdWithDetailsAsync(variantId);

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

            var cart = await _unitOfWork.Carts.GetCartByCustomerIdAsync(customerId, includeDetails: true);

            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Carts.AddAsync(cart);
                await _unitOfWork.SaveChangesAsync();
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
                await _unitOfWork.Carts.AddCartItemAsync(item);
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

            await _unitOfWork.SaveChangesAsync();

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
            var item = await _unitOfWork.Carts.GetCartItemByIdWithDetailsAsync(cartItemId);

            if (item == null || item.Cart?.CustomerId != customerId)
            {
                return new CartResult { Success = false, Message = "Item not found in your cart." };
            }

            if (quantity <= 0)
            {
                _unitOfWork.Carts.RemoveCartItem(item);
                await _unitOfWork.SaveChangesAsync();
                var count = await GetCartCountAsync(customerId);
                return new CartResult { Success = true, Message = "Item removed from cart.", CartCount = count };
            }

            var available = item.Variant?.Inventory?.AvailableQuantity ?? 0;
            if (available <= 0)
            {
                _unitOfWork.Carts.RemoveCartItem(item);
                await _unitOfWork.SaveChangesAsync();
                return new CartResult { Success = false, Message = "This product is out of stock and was removed from your cart." };
            }

            if (quantity > available)
            {
                item.Quantity = available;
                _unitOfWork.Carts.UpdateCartItem(item);
                await _unitOfWork.SaveChangesAsync();
                return new CartResult { Success = false, Message = $"Only {available} item(s) available in stock. Quantity adjusted." };
            }

            item.Quantity = quantity;
            _unitOfWork.Carts.UpdateCartItem(item);
            await _unitOfWork.SaveChangesAsync();

            var newCount = await GetCartCountAsync(customerId);
            return new CartResult { Success = true, Message = "Cart updated successfully.", CartCount = newCount };
        }

        public async Task<CartResult> RemoveItemAsync(Guid customerId, Guid cartItemId)
        {
            var item = await _unitOfWork.Carts.GetCartItemByIdWithDetailsAsync(cartItemId);
            if (item != null && item.Cart?.CustomerId == customerId)
            {
                _unitOfWork.Carts.RemoveCartItem(item);
                await _unitOfWork.SaveChangesAsync();
            }
            var count = await GetCartCountAsync(customerId);
            return new CartResult { Success = true, Message = "Item removed from cart.", CartCount = count };
        }

        public async Task<CartResult> ChangeVariantAsync(Guid customerId, Guid cartItemId, Guid newVariantId)
        {
            var item = await _unitOfWork.Carts.GetCartItemByIdWithDetailsAsync(cartItemId);
            if (item == null || item.Cart?.CustomerId != customerId)
            {
                return new CartResult { Success = false, Message = "Cart item not found." };
            }

            if (item.VariantId == newVariantId)
            {
                return new CartResult { Success = true, Message = "Variant is already selected.", CartCount = await GetCartCountAsync(customerId) };
            }

            var newVariant = await _unitOfWork.Products.GetVariantByIdWithDetailsAsync(newVariantId);
            if (newVariant == null)
            {
                return new CartResult { Success = false, Message = "Selected variant does not exist." };
            }

            var available = newVariant.Inventory?.AvailableQuantity ?? 0;
            if (available <= 0)
            {
                return new CartResult { Success = false, Message = "Selected option is out of stock." };
            }

            var cart = await _unitOfWork.Carts.GetCartByCustomerIdAsync(customerId, includeDetails: true);
            var existingItemWithVariant = cart?.Items.FirstOrDefault(i => i.VariantId == newVariantId && i.Id != cartItemId);

            if (existingItemWithVariant != null)
            {
                var combinedQty = existingItemWithVariant.Quantity + item.Quantity;
                if (combinedQty > available)
                {
                    combinedQty = available;
                }
                existingItemWithVariant.Quantity = combinedQty;
                _unitOfWork.Carts.UpdateCartItem(existingItemWithVariant);
                _unitOfWork.Carts.RemoveCartItem(item);
            }
            else
            {
                if (item.Quantity > available)
                {
                    item.Quantity = available;
                }
                item.VariantId = newVariantId;
                _unitOfWork.Carts.UpdateCartItem(item);
            }

            await _unitOfWork.SaveChangesAsync();
            var count = await GetCartCountAsync(customerId);
            return new CartResult
            {
                Success = true,
                Message = $"Updated to {newVariant.Size?.Name ?? ""} / {newVariant.Color?.Name ?? ""}.",
                CartCount = count
            };
        }

        public async Task<int> GetCartCountAsync(Guid customerId)
        {
            return await _unitOfWork.Carts.GetCartItemCountAsync(customerId);
        }

        public async Task ClearCartAsync(Guid customerId)
        {
            var cart = await _unitOfWork.Carts.GetCartByCustomerIdAsync(customerId, includeDetails: true);
            if (cart != null && cart.Items.Any())
            {
                _unitOfWork.Carts.RemoveCartItems(cart.Items);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<Cart?> GetAdminCustomerCartAsync(Guid customerId)
        {
            return await _unitOfWork.Carts.GetCartByCustomerIdAsync(customerId, includeDetails: true);
        }

        public async Task<List<CartItem>> GetAllCartItemsAsync()
        {
            var items = await _unitOfWork.Carts.GetAllCartItemsWithDetailsAsync();
            return items.ToList();
        }

        public async Task<CartItem?> GetCartItemByIdAsync(Guid id)
        {
            return await _unitOfWork.Carts.GetCartItemByIdWithDetailsAsync(id);
        }

        public async Task<CartItem?> GetFirstCartItemAsync()
        {
            return await _unitOfWork.Carts.GetFirstCartItemWithDetailsAsync();
        }

        public async Task<CartItem?> EnsureSampleCartItemsAsync(Guid? customerId = null)
        {
            var existingItem = await _unitOfWork.Carts.GetFirstCartItemWithDetailsAsync();
            if (existingItem != null && (!customerId.HasValue || existingItem.Cart?.CustomerId == customerId.Value))
            {
                return existingItem;
            }

            Customer? customer = null;
            if (customerId.HasValue)
            {
                customer = await _unitOfWork.Customers.GetByIdAsync(customerId.Value);
            }
            if (customer == null)
            {
                customer = (await _unitOfWork.Customers.GetAllAsync()).FirstOrDefault();
            }

            if (customer == null)
            {
                customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@gmail.com",
                    Phone = "012345678",
                    Status = CustomerStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Customers.AddAsync(customer);
                await _unitOfWork.SaveChangesAsync();
            }

            var cart = await _unitOfWork.Carts.GetCartByCustomerIdAsync(customer.Id, includeDetails: true);
            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Carts.AddAsync(cart);
                await _unitOfWork.SaveChangesAsync();
            }

            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync();

            if (variant == null)
            {
                return null;
            }

            var newItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                VariantId = variant.Id,
                Quantity = 2
            };

            await _unitOfWork.Carts.AddCartItemAsync(newItem);
            await _unitOfWork.SaveChangesAsync();

            return await _unitOfWork.Carts.GetCartItemByIdWithDetailsAsync(newItem.Id);
        }

        public async Task<ServiceResult> CreateCartItemAsync(CartItem item)
        {
            if (item.Id == Guid.Empty)
            {
                item.Id = Guid.NewGuid();
            }
            await _unitOfWork.Carts.AddCartItemAsync(item);
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Ok("Cart item created successfully.");
        }

        public async Task<ServiceResult> UpdateCartItemAsync(Guid id, CartItem item)
        {
            var existing = await _unitOfWork.Carts.GetCartItemByIdWithDetailsAsync(id);
            if (existing == null)
            {
                return ServiceResult.Fail("Cart item not found.");
            }

            existing.CartId = item.CartId;
            existing.VariantId = item.VariantId;
            existing.Quantity = item.Quantity;

            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Ok("Cart item updated successfully.");
        }

        public async Task<ServiceResult> DeleteCartItemAsync(Guid id)
        {
            var item = await _unitOfWork.Carts.GetCartItemByIdWithDetailsAsync(id);
            if (item == null)
            {
                return ServiceResult.Fail("Cart item not found.");
            }

            _unitOfWork.Carts.RemoveCartItem(item);
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Ok("Cart item deleted successfully.");
        }
    }
}
