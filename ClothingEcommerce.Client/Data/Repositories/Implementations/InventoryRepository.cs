using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data.Repositories.Interfaces;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Implementations
{
    public class InventoryRepository : Repository<Inventory>, IInventoryRepository
    {
        public InventoryRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Inventory?> GetByVariantIdAsync(Guid variantId)
        {
            return await _dbSet
                .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(i => i.ProductVariantId == variantId);
        }

        public async Task<IReadOnlyList<Inventory>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Color)
                .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Size)
                .OrderBy(i => i.ProductVariant.Product.Name)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Inventory>> GetLowStockInventoriesAsync(int threshold)
        {
            return await _dbSet
                .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                .Where(i => (i.Quantity - i.ReservedQuantity) <= threshold)
                .ToListAsync();
        }
    }
}
