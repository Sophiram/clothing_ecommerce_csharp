using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly AppDbContext _context;

        public InventoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Inventory>> GetInventoriesAsync(Guid? productId)
        {
            var query = _context.Inventories
                .AsNoTracking()
                .Include(i => i.ProductVariant).ThenInclude(v => v.Product).ThenInclude(p => p.Images)
                .Include(i => i.ProductVariant).ThenInclude(v => v.Size)
                .Include(i => i.ProductVariant).ThenInclude(v => v.Color)
                .AsQueryable();

            if (productId.HasValue)
            {
                query = query.Where(i => i.ProductVariant.ProductId == productId.Value);
            }

            return await query
                .OrderBy(i => i.ProductVariant.Product.Name)
                .ThenBy(i => i.ProductVariant.Size.Name)
                .ThenBy(i => i.ProductVariant.Color.Name)
                .ToListAsync();
        }

        public async Task<Inventory?> GetInventoryDetailsAsync(Guid id)
        {
            return await _context.Inventories
                .AsNoTracking()
                .Include(i => i.ProductVariant).ThenInclude(v => v.Product).ThenInclude(p => p.Images)
                .Include(i => i.ProductVariant).ThenInclude(v => v.Size)
                .Include(i => i.ProductVariant).ThenInclude(v => v.Color)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
        }

        public async Task<List<VariantDropdownItem>> GetVariantDropdownItemsAsync()
        {
            var variants = await _context.ProductVariants
                .AsNoTracking()
                .Include(v => v.Product)
                .Include(v => v.Size)
                .Include(v => v.Color)
                .OrderBy(v => v.Product.Name)
                .ThenBy(v => v.Size.Name)
                .ThenBy(v => v.Color.Name)
                .ToListAsync();

            return variants.Select(v => new VariantDropdownItem
            {
                Id = v.Id,
                Name = $"{v.Product.Name} | Size: {v.Size.Name} | Color: {v.Color.Name} | SKU: {v.SKU}"
            }).ToList();
        }

        public async Task<ServiceResult> CreateInventoryAsync(Inventory inventory)
        {
            var errors = new List<string>();

            if (inventory.ProductVariantId == Guid.Empty)
            {
                errors.Add("Please select a product variant.");
            }
            else
            {
                var variantExists = await _context.ProductVariants.AnyAsync(v => v.Id == inventory.ProductVariantId);
                if (!variantExists)
                {
                    errors.Add("Selected product variant does not exist.");
                }
            }

            if (inventory.Quantity < 0)
            {
                errors.Add("Quantity cannot be negative.");
            }

            if (inventory.ReservedQuantity < 0)
            {
                errors.Add("Reserved quantity cannot be negative.");
            }

            if (inventory.ReservedQuantity > inventory.Quantity)
            {
                errors.Add("Reserved quantity cannot be greater than total quantity.");
            }

            if (inventory.ProductVariantId != Guid.Empty)
            {
                var exists = await _context.Inventories.AnyAsync(i => i.ProductVariantId == inventory.ProductVariantId);
                if (exists)
                {
                    errors.Add("This product variant already has an inventory record.");
                }
            }

            if (errors.Any())
            {
                return ServiceResult.Fail(errors);
            }

            inventory.Id = Guid.NewGuid();
            inventory.UpdatedAt = DateTime.UtcNow;

            _context.Inventories.Add(inventory);

            var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == inventory.ProductVariantId);
            if (variant != null)
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == variant.ProductId);
                if (product != null)
                {
                    product.ModifiedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Inventory was created successfully.");
        }

        public async Task<ServiceResult> UpdateInventoryAsync(Guid id, Inventory inventory)
        {
            var errors = new List<string>();

            if (inventory.Quantity < 0)
            {
                errors.Add("Quantity cannot be negative.");
            }

            if (inventory.ReservedQuantity < 0)
            {
                errors.Add("Reserved quantity cannot be negative.");
            }

            if (inventory.ReservedQuantity > inventory.Quantity)
            {
                errors.Add("Reserved quantity cannot be greater than total quantity.");
            }

            if (inventory.ProductVariantId == Guid.Empty)
            {
                errors.Add("Please select a product variant.");
            }

            var duplicate = await _context.Inventories.AnyAsync(i => i.Id != id && i.ProductVariantId == inventory.ProductVariantId);
            if (duplicate)
            {
                errors.Add("This product variant already has an inventory record.");
            }

            if (errors.Any())
            {
                return ServiceResult.Fail(errors);
            }

            var existing = await _context.Inventories.FirstOrDefaultAsync(i => i.Id == id);
            if (existing == null)
            {
                return ServiceResult.Fail("Inventory record not found.");
            }

            var oldVariantId = existing.ProductVariantId;

            existing.ProductVariantId = inventory.ProductVariantId;
            existing.Quantity = inventory.Quantity;
            existing.ReservedQuantity = inventory.ReservedQuantity;
            existing.UpdatedAt = DateTime.UtcNow;

            var variantIds = new[] { oldVariantId, existing.ProductVariantId };
            var variants = await _context.ProductVariants.Where(v => variantIds.Contains(v.Id)).ToListAsync();
            var productIds = variants.Select(v => v.ProductId).Distinct().ToList();
            var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

            foreach (var product in products)
            {
                product.ModifiedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Inventory was updated successfully.");
        }

        public async Task<ServiceResult> DeleteInventoryAsync(Guid id)
        {
            var inventory = await _context.Inventories
                .Include(i => i.ProductVariant)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null)
            {
                return ServiceResult.Fail("Inventory record not found.");
            }

            try
            {
                var productId = inventory.ProductVariant.ProductId;

                _context.Inventories.Remove(inventory);

                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
                if (product != null)
                {
                    product.ModifiedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return ServiceResult.Ok("Inventory was deleted successfully.");
            }
            catch (DbUpdateException)
            {
                return ServiceResult.Fail("Cannot delete this inventory record because it is being used by another record.");
            }
        }
    }
}
