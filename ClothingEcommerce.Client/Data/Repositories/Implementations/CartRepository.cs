using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Data.Repositories.Interfaces;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Implementations
{
    public class CartRepository : Repository<Cart>, ICartRepository
    {
        public CartRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Cart?> GetCartByCustomerIdAsync(Guid customerId, bool includeDetails = true)
        {
            IQueryable<Cart> query = _dbSet;

            if (includeDetails)
            {
                query = query
                    .Include(c => c.Customer)
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
                            .ThenInclude(v => v.Color)
                    .Include(c => c.Items)
                        .ThenInclude(i => i.Variant)
                            .ThenInclude(v => v.Size)
                    .Include(c => c.Items)
                        .ThenInclude(i => i.Variant)
                            .ThenInclude(v => v.Inventory);
            }

            return await query.FirstOrDefaultAsync(c => c.CustomerId == customerId);
        }

        public async Task<Cart?> GetCartByIdWithDetailsAsync(Guid cartId)
        {
            return await _dbSet
                .Include(c => c.Customer)
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
                        .ThenInclude(v => v.Color)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Size)
                .FirstOrDefaultAsync(c => c.Id == cartId);
        }

        public async Task<CartItem?> GetCartItemByIdWithDetailsAsync(Guid cartItemId)
        {
            return await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images)
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Brand)
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Color)
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Size)
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Inventory)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);
        }

        public async Task<IReadOnlyList<CartItem>> GetAllCartItemsWithDetailsAsync()
        {
            return await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images)
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Brand)
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Color)
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Size)
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Inventory)
                .OrderByDescending(ci => ci.Quantity)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<CartItem?> GetFirstCartItemWithDetailsAsync()
        {
            return await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images)
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Brand)
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Color)
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Size)
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Inventory)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetCartItemCountAsync(Guid customerId)
        {
            return await _context.CartItems
                .Where(ci => ci.Cart.CustomerId == customerId)
                .SumAsync(ci => (int?)ci.Quantity) ?? 0;
        }

        public async Task AddCartItemAsync(CartItem item)
        {
            await _context.CartItems.AddAsync(item);
        }

        public void UpdateCartItem(CartItem item)
        {
            var trackedEntry = _context.ChangeTracker.Entries<CartItem>().FirstOrDefault(e => e.Entity.Id == item.Id);
            if (trackedEntry == null)
            {
                _context.CartItems.Update(item);
            }
        }

        public void RemoveCartItem(CartItem item)
        {
            var trackedEntry = _context.ChangeTracker.Entries<CartItem>().FirstOrDefault(e => e.Entity.Id == item.Id);
            if (trackedEntry != null)
            {
                _context.CartItems.Remove(trackedEntry.Entity);
            }
            else
            {
                var stub = new CartItem { Id = item.Id };
                _context.CartItems.Attach(stub);
                _context.CartItems.Remove(stub);
            }
        }

        public void RemoveCartItems(IEnumerable<CartItem> items)
        {
            foreach (var item in items.ToList())
            {
                RemoveCartItem(item);
            }
        }
    }
}
