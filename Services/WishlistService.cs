using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly AppDbContext _context;

        public WishlistService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Wishlist>> GetAllWishlistsAsync(string? search = null)
        {
            var query = _context.Wishlists
                .AsNoTracking()
                .Include(w => w.Customer)
                .Include(w => w.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(w =>
                    (w.Customer != null && (
                        w.Customer.FirstName.Contains(search) ||
                        w.Customer.LastName.Contains(search) ||
                        w.Customer.Email.Contains(search)
                    ))
                );
            }

            return await query
                .OrderByDescending(w => w.Items.Count)
                .ToListAsync();
        }

        public async Task<Wishlist?> GetWishlistByIdAsync(Guid id)
        {
            return await _context.Wishlists
                .AsNoTracking()
                .Include(w => w.Customer)
                .Include(w => w.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)
                .Include(w => w.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Size)
                .Include(w => w.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Color)
                .Include(w => w.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Inventory)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<Wishlist> GetOrCreateCustomerWishlistAsync(Guid customerId)
        {
            var wishlist = await _context.Wishlists
                .Include(w => w.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)
                .Include(w => w.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Inventory)
                .Include(w => w.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Size)
                .Include(w => w.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Color)
                .FirstOrDefaultAsync(w => w.CustomerId == customerId);

            if (wishlist == null)
            {
                wishlist = new Wishlist
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId
                };
                _context.Wishlists.Add(wishlist);
                await _context.SaveChangesAsync();
            }

            return wishlist;
        }

        public async Task<(bool Success, bool IsWishlisted, string Message)> ToggleWishlistAsync(Guid customerId, Guid variantId)
        {
            var wishlist = await GetOrCreateCustomerWishlistAsync(customerId);
            var item = wishlist.Items.FirstOrDefault(i => i.VariantId == variantId);

            bool isWishlisted;
            string message;

            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                isWishlisted = false;
                message = "Product removed from wishlist.";
            }
            else
            {
                _context.WishlistItems.Add(new WishlistItem
                {
                    Id = Guid.NewGuid(),
                    WishlistId = wishlist.Id,
                    VariantId = variantId
                });
                isWishlisted = true;
                message = "Product added to wishlist.";
            }

            await _context.SaveChangesAsync();
            return (true, isWishlisted, message);
        }

        public async Task<(bool Success, string Message)> AddToWishlistAsync(Guid customerId, Guid variantId)
        {
            var variantExists = await _context.ProductVariants.AnyAsync(v => v.Id == variantId);
            if (!variantExists) return (false, "Product variant was not found.");

            var wishlist = await GetOrCreateCustomerWishlistAsync(customerId);
            var alreadyExists = wishlist.Items.Any(i => i.VariantId == variantId);

            if (!alreadyExists)
            {
                _context.WishlistItems.Add(new WishlistItem
                {
                    Id = Guid.NewGuid(),
                    WishlistId = wishlist.Id,
                    VariantId = variantId
                });
                await _context.SaveChangesAsync();
                return (true, "Product added to wishlist.");
            }

            return (false, "Product is already in your wishlist.");
        }

        public async Task<(bool Success, string Message)> RemoveFromWishlistAsync(Guid customerId, Guid? id, Guid? variantId)
        {
            var wishlist = await GetOrCreateCustomerWishlistAsync(customerId);
            WishlistItem? item = null;

            if (id.HasValue && id.Value != Guid.Empty)
            {
                item = wishlist.Items.FirstOrDefault(i => i.Id == id.Value);
            }
            else if (variantId.HasValue && variantId.Value != Guid.Empty)
            {
                item = wishlist.Items.FirstOrDefault(i => i.VariantId == variantId.Value);
            }

            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
                return (true, "Product removed from wishlist.");
            }

            return (false, "Item not found in your wishlist.");
        }

        public async Task<(bool Success, string Message)> MoveToCartAsync(Guid customerId, Guid wishlistItemId)
        {
            var wishlistItem = await _context.WishlistItems
                .Include(i => i.Variant)
                    .ThenInclude(v => v.Inventory)
                .Include(i => i.Wishlist)
                .FirstOrDefaultAsync(i => i.Id == wishlistItemId && i.Wishlist.CustomerId == customerId);

            if (wishlistItem == null)
            {
                return (false, "Wishlist item was not found.");
            }

            var available = wishlistItem.Variant?.Inventory?.AvailableQuantity ?? 0;
            if (available <= 0)
            {
                return (false, "This product is currently out of stock.");
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

            var cartItem = cart.Items.FirstOrDefault(i => i.VariantId == wishlistItem.VariantId);
            if (cartItem != null)
            {
                cartItem.Quantity += 1;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    VariantId = wishlistItem.VariantId,
                    Quantity = 1
                });
            }

            _context.WishlistItems.Remove(wishlistItem);
            await _context.SaveChangesAsync();

            return (true, "Item moved to your shopping bag!");
        }

        public async Task<bool> ClearWishlistAsync(Guid customerId)
        {
            var items = await _context.WishlistItems
                .Where(w => w.Wishlist.CustomerId == customerId)
                .ToListAsync();

            if (items.Any())
            {
                _context.WishlistItems.RemoveRange(items);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> DeleteWishlistAsync(Guid id)
        {
            var wishlist = await _context.Wishlists
                .Include(w => w.Items)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (wishlist == null) return false;

            _context.WishlistItems.RemoveRange(wishlist.Items);
            _context.Wishlists.Remove(wishlist);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<WishlistItem>> GetAllWishlistItemsAsync()
        {
            return await _context.WishlistItems
                .Include(w => w.Wishlist)
                .Include(w => w.Variant)
                    .ThenInclude(v => v.Product)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<WishlistItem?> GetWishlistItemByIdAsync(Guid id)
        {
            return await _context.WishlistItems
                .Include(w => w.Wishlist)
                .Include(w => w.Variant)
                    .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<ServiceResult> CreateWishlistItemAsync(WishlistItem item)
        {
            try
            {
                if (item.Id == Guid.Empty)
                {
                    item.Id = Guid.NewGuid();
                }
                _context.WishlistItems.Add(item);
                await _context.SaveChangesAsync();
                return ServiceResult.Ok("Wishlist item created successfully.");
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Failed to create wishlist item: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateWishlistItemAsync(Guid id, WishlistItem item)
        {
            try
            {
                var existing = await _context.WishlistItems.FindAsync(id);
                if (existing == null)
                {
                    return ServiceResult.Fail("Wishlist item not found.");
                }

                existing.WishlistId = item.WishlistId;
                existing.VariantId = item.VariantId;

                await _context.SaveChangesAsync();
                return ServiceResult.Ok("Wishlist item updated successfully.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await WishlistItemExistsAsync(id))
                {
                    return ServiceResult.Fail("Wishlist item not found.");
                }
                throw;
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Failed to update wishlist item: {ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteWishlistItemAsync(Guid id)
        {
            try
            {
                var item = await _context.WishlistItems.FindAsync(id);
                if (item == null)
                {
                    return ServiceResult.Fail("Wishlist item not found.");
                }

                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
                return ServiceResult.Ok("Wishlist item deleted successfully.");
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Failed to delete wishlist item: {ex.Message}");
            }
        }

        public async Task<bool> WishlistItemExistsAsync(Guid id)
        {
            return await _context.WishlistItems.AnyAsync(e => e.Id == id);
        }
    }
}
