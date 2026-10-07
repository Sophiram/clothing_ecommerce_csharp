using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data.Repositories.Interfaces;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Implementations
{
    public class WishlistRepository : Repository<Wishlist>, IWishlistRepository
    {
        public WishlistRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Wishlist?> GetWishlistByCustomerIdAsync(Guid customerId, bool includeDetails = true)
        {
            IQueryable<Wishlist> query = _dbSet;
            if (includeDetails)
            {
                query = query
                    .Include(w => w.Items)
                        .ThenInclude(i => i.Variant)
                            .ThenInclude(v => v.Product)
                                .ThenInclude(p => p.Images)
                    .Include(w => w.Items)
                        .ThenInclude(i => i.Variant)
                            .ThenInclude(v => v.Color)
                    .Include(w => w.Items)
                        .ThenInclude(i => i.Variant)
                            .ThenInclude(v => v.Size)
                    .Include(w => w.Items)
                        .ThenInclude(i => i.Variant)
                            .ThenInclude(v => v.Inventory);
            }

            return await query.FirstOrDefaultAsync(w => w.CustomerId == customerId);
        }

        public async Task<bool> IsVariantWishlistedAsync(Guid customerId, Guid variantId)
        {
            return await _context.WishlistItems
                .AnyAsync(wi => wi.Wishlist.CustomerId == customerId && wi.VariantId == variantId);
        }

        public async Task<IReadOnlyList<Guid>> GetWishlistedVariantIdsAsync(Guid customerId)
        {
            return await _context.WishlistItems
                .Where(wi => wi.Wishlist.CustomerId == customerId)
                .Select(wi => wi.VariantId)
                .ToListAsync();
        }

        public async Task AddWishlistItemAsync(WishlistItem item)
        {
            await _context.WishlistItems.AddAsync(item);
        }

        public void RemoveWishlistItem(WishlistItem item)
        {
            _context.WishlistItems.Remove(item);
        }
    }
}
