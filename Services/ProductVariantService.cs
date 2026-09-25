using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class ProductVariantService : IProductVariantService
    {
        private readonly AppDbContext _context;

        public ProductVariantService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProductVariant>> GetVariantsAsync(Guid? productId)
        {
            var query = _context.ProductVariants
                .AsNoTracking()
                .Include(v => v.Product).ThenInclude(p => p.Images)
                .Include(v => v.Size)
                .Include(v => v.Color)
                .Include(v => v.Inventory)
                .AsQueryable();

            if (productId.HasValue)
            {
                query = query.Where(v => v.ProductId == productId.Value);
            }

            return await query
                .OrderBy(v => v.Product.Name)
                .ThenBy(v => v.Size.Name)
                .ThenBy(v => v.Color.Name)
                .ToListAsync();
        }

        public async Task<ProductVariant?> GetVariantDetailsAsync(Guid id)
        {
            return await _context.ProductVariants
                .AsNoTracking()
                .Include(v => v.Product).ThenInclude(p => p.Images)
                .Include(v => v.Size)
                .Include(v => v.Color)
                .Include(v => v.Inventory)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<(List<Product> Products, List<Size> Sizes, List<Color> Colors)> GetVariantDropdownDataAsync()
        {
            var products = await _context.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
            var sizes = await _context.Sizes.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
            var colors = await _context.Colors.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            return (products, sizes, colors);
        }

        public async Task<ServiceResult> CreateVariantAsync(ProductVariant variant, int quantity)
        {
            var errors = new List<string>();

            if (variant.ProductId == Guid.Empty)
            {
                errors.Add("Please select a product.");
            }
            else if (!await _context.Products.AnyAsync(p => p.Id == variant.ProductId))
            {
                errors.Add("Selected product does not exist.");
            }

            if (variant.SizeId == Guid.Empty)
            {
                errors.Add("Please select a size.");
            }
            else if (!await _context.Sizes.AnyAsync(s => s.Id == variant.SizeId))
            {
                errors.Add("Selected size does not exist.");
            }

            if (variant.ColorId == Guid.Empty)
            {
                errors.Add("Please select a color.");
            }
            else if (!await _context.Colors.AnyAsync(c => c.Id == variant.ColorId))
            {
                errors.Add("Selected color does not exist.");
            }

            if (string.IsNullOrWhiteSpace(variant.SKU))
            {
                errors.Add("SKU is required.");
            }

            if (variant.Price < 0)
            {
                errors.Add("Price cannot be negative.");
            }

            if (variant.CompareAtPrice.HasValue && variant.CompareAtPrice.Value < 0)
            {
                errors.Add("Compare-at price cannot be negative.");
            }

            if (quantity < 0)
            {
                errors.Add("Inventory quantity cannot be negative.");
            }

            if (variant.ProductId != Guid.Empty && variant.SizeId != Guid.Empty && variant.ColorId != Guid.Empty)
            {
                var duplicate = await _context.ProductVariants.AnyAsync(v =>
                    v.ProductId == variant.ProductId &&
                    v.SizeId == variant.SizeId &&
                    v.ColorId == variant.ColorId);

                if (duplicate)
                {
                    errors.Add("This product already has this Size + Color variant.");
                }
            }

            if (errors.Any())
            {
                return ServiceResult.Fail(errors);
            }

            variant.Id = Guid.NewGuid();
            variant.SKU = variant.SKU.Trim();

            _context.ProductVariants.Add(variant);

            var inventory = new Inventory
            {
                Id = Guid.NewGuid(),
                ProductVariantId = variant.Id,
                Quantity = quantity,
                ReservedQuantity = 0,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Inventories.Add(inventory);

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == variant.ProductId);
            if (product != null)
            {
                product.ModifiedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Ok($"Variant '{variant.SKU}' was created successfully.");
        }

        public async Task<ServiceResult> UpdateVariantAsync(Guid id, ProductVariant variant, int quantity)
        {
            var errors = new List<string>();

            if (variant.ProductId == Guid.Empty)
            {
                errors.Add("Please select a product.");
            }

            if (variant.SizeId == Guid.Empty)
            {
                errors.Add("Please select a size.");
            }

            if (variant.ColorId == Guid.Empty)
            {
                errors.Add("Please select a color.");
            }

            if (string.IsNullOrWhiteSpace(variant.SKU))
            {
                errors.Add("SKU is required.");
            }

            if (variant.Price < 0)
            {
                errors.Add("Price cannot be negative.");
            }

            if (variant.CompareAtPrice.HasValue && variant.CompareAtPrice.Value < 0)
            {
                errors.Add("Compare-at price cannot be negative.");
            }

            if (quantity < 0)
            {
                errors.Add("Inventory quantity cannot be negative.");
            }

            var duplicate = await _context.ProductVariants.AnyAsync(v =>
                v.Id != id &&
                v.ProductId == variant.ProductId &&
                v.SizeId == variant.SizeId &&
                v.ColorId == variant.ColorId);

            if (duplicate)
            {
                errors.Add("This product already has this Size + Color variant.");
            }

            if (errors.Any())
            {
                return ServiceResult.Fail(errors);
            }

            var existing = await _context.ProductVariants
                .Include(v => v.Inventory)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (existing == null)
            {
                return ServiceResult.Fail("Product variant not found.");
            }

            var oldProductId = existing.ProductId;

            existing.ProductId = variant.ProductId;
            existing.SizeId = variant.SizeId;
            existing.ColorId = variant.ColorId;
            existing.SKU = variant.SKU.Trim();
            existing.Price = variant.Price;
            existing.CompareAtPrice = variant.CompareAtPrice;
            existing.Status = variant.Status;

            if (existing.Inventory == null)
            {
                existing.Inventory = new Inventory
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = existing.Id,
                    Quantity = quantity,
                    ReservedQuantity = 0,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Inventories.Add(existing.Inventory);
            }
            else
            {
                existing.Inventory.Quantity = quantity;
                existing.Inventory.UpdatedAt = DateTime.UtcNow;
            }

            var productIds = new[] { oldProductId, existing.ProductId };
            var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

            foreach (var product in products)
            {
                product.ModifiedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Ok($"Variant '{existing.SKU}' was updated successfully.");
        }

        public async Task<ServiceResult> DeleteVariantAsync(Guid id)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.Inventory)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (variant == null)
            {
                return ServiceResult.Fail("Product variant not found.");
            }

            var productId = variant.ProductId;
            var sku = variant.SKU;

            try
            {
                if (variant.Inventory != null)
                {
                    _context.Inventories.Remove(variant.Inventory);
                }

                _context.ProductVariants.Remove(variant);

                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
                if (product != null)
                {
                    product.ModifiedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return ServiceResult.Ok($"Variant '{sku}' was deleted successfully.");
            }
            catch (DbUpdateException)
            {
                return ServiceResult.Fail("Cannot delete this variant because it is being used by other records.");
            }
        }
    }
}
