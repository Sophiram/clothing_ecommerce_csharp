using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductVariantsController : Controller
{
    private readonly AppDbContext _context;

    public ProductVariantsController(AppDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // INDEX
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index(Guid? productId)
    {
        var query = _context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .Include(v => v.Size)
            .Include(v => v.Color)
            .Include(v => v.Inventory)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(v => v.ProductId == productId.Value);
        }

        var variants = await query
            .OrderBy(v => v.Product.Name)
            .ThenBy(v => v.Size.Name)
            .ThenBy(v => v.Color.Name)
            .ToListAsync();

        // Filter dropdown (pre-selected)
        ViewBag.Products = new SelectList(
            await _context.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync(),
            "Id", "Name", productId);

        // Plain lookup lists for the Create/Edit modals (no pre-selection)
        ViewBag.AllProducts = await _context.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
        ViewBag.AllSizes = await _context.Sizes.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
        ViewBag.AllColors = await _context.Colors.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        ViewBag.StatusValues = Enum.GetValues(typeof(VariantStatus)).Cast<VariantStatus>().ToList();

        ViewBag.SelectedProductId = productId;

        return View(variants);
    }

    // =========================================================
    // DETAILS
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue)
        {
            TempData["Error"] = "Variant ID is missing.";
            return RedirectToAction(nameof(Index));
        }

        var variant = await _context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product).ThenInclude(p => p.Images)
            .Include(v => v.Size)
            .Include(v => v.Color)
            .Include(v => v.Inventory)
            .FirstOrDefaultAsync(v => v.Id == id.Value);

        if (variant == null)
        {
            TempData["Error"] = "Product variant not found.";
            return RedirectToAction(nameof(Index));
        }

        // Needed for the Edit modal dropdowns
        ViewBag.AllProducts = await _context.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
        ViewBag.AllSizes = await _context.Sizes.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
        ViewBag.AllColors = await _context.Colors.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        ViewBag.StatusValues = Enum.GetValues(typeof(VariantStatus)).Cast<VariantStatus>().ToList();

        return View(variant);
    }

    // =========================================================
    // CREATE - POST (AJAX)
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductVariant variant, int quantity = 0)
    {
        ModelState.Remove(nameof(ProductVariant.Product));
        ModelState.Remove(nameof(ProductVariant.Size));
        ModelState.Remove(nameof(ProductVariant.Color));
        ModelState.Remove(nameof(ProductVariant.Inventory));
        ModelState.Remove(nameof(ProductVariant.Id));

        if (variant.ProductId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(ProductVariant.ProductId), "Please select a product.");
        }
        else if (!await _context.Products.AnyAsync(p => p.Id == variant.ProductId))
        {
            ModelState.AddModelError(nameof(ProductVariant.ProductId), "Selected product does not exist.");
        }

        if (variant.SizeId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(ProductVariant.SizeId), "Please select a size.");
        }
        else if (!await _context.Sizes.AnyAsync(s => s.Id == variant.SizeId))
        {
            ModelState.AddModelError(nameof(ProductVariant.SizeId), "Selected size does not exist.");
        }

        if (variant.ColorId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(ProductVariant.ColorId), "Please select a color.");
        }
        else if (!await _context.Colors.AnyAsync(c => c.Id == variant.ColorId))
        {
            ModelState.AddModelError(nameof(ProductVariant.ColorId), "Selected color does not exist.");
        }

        if (string.IsNullOrWhiteSpace(variant.SKU))
        {
            ModelState.AddModelError(nameof(ProductVariant.SKU), "SKU is required.");
        }

        if (variant.Price < 0)
        {
            ModelState.AddModelError(nameof(ProductVariant.Price), "Price cannot be negative.");
        }

        if (quantity < 0)
        {
            ModelState.AddModelError("quantity", "Inventory quantity cannot be negative.");
        }

        if (variant.ProductId != Guid.Empty && variant.SizeId != Guid.Empty && variant.ColorId != Guid.Empty)
        {
            var duplicate = await _context.ProductVariants.AnyAsync(v =>
                v.ProductId == variant.ProductId &&
                v.SizeId == variant.SizeId &&
                v.ColorId == variant.ColorId);

            if (duplicate)
            {
                ModelState.AddModelError("", "This product already has this Size + Color variant.");
            }
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, errors });
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

        return Ok(new { success = true, message = $"Variant '{variant.SKU}' was created successfully." });
    }

    // =========================================================
    // EDIT - POST (AJAX)
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProductVariant variant, int quantity = 0)
    {
        if (id != variant.Id)
        {
            return BadRequest(new { success = false, errors = new[] { "Invalid variant ID." } });
        }

        ModelState.Remove(nameof(ProductVariant.Product));
        ModelState.Remove(nameof(ProductVariant.Size));
        ModelState.Remove(nameof(ProductVariant.Color));
        ModelState.Remove(nameof(ProductVariant.Inventory));

        if (variant.ProductId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(ProductVariant.ProductId), "Please select a product.");
        }

        if (variant.SizeId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(ProductVariant.SizeId), "Please select a size.");
        }

        if (variant.ColorId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(ProductVariant.ColorId), "Please select a color.");
        }

        if (string.IsNullOrWhiteSpace(variant.SKU))
        {
            ModelState.AddModelError(nameof(ProductVariant.SKU), "SKU is required.");
        }

        if (variant.Price < 0)
        {
            ModelState.AddModelError(nameof(ProductVariant.Price), "Price cannot be negative.");
        }

        if (quantity < 0)
        {
            ModelState.AddModelError("quantity", "Inventory quantity cannot be negative.");
        }

        var duplicate = await _context.ProductVariants.AnyAsync(v =>
            v.Id != id &&
            v.ProductId == variant.ProductId &&
            v.SizeId == variant.SizeId &&
            v.ColorId == variant.ColorId);

        if (duplicate)
        {
            ModelState.AddModelError("", "This product already has this Size + Color variant.");
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, errors });
        }

        var existing = await _context.ProductVariants
            .Include(v => v.Inventory)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (existing == null)
        {
            return NotFound(new { success = false, errors = new[] { "Product variant not found." } });
        }

        var oldProductId = existing.ProductId;

        existing.ProductId = variant.ProductId;
        existing.SizeId = variant.SizeId;
        existing.ColorId = variant.ColorId;
        existing.SKU = variant.SKU.Trim();
        existing.Price = variant.Price;
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

        return Ok(new { success = true, message = $"Variant '{existing.SKU}' was updated successfully." });
    }

    // =========================================================
    // DELETE - POST (AJAX)
    // =========================================================

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var variant = await _context.ProductVariants
            .Include(v => v.Inventory)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (variant == null)
        {
            return NotFound(new { success = false, errors = new[] { "Product variant not found." } });
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

            return Ok(new { success = true, message = $"Variant '{sku}' was deleted successfully." });
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { success = false, errors = new[] { "Cannot delete this variant because it is being used by other records." } });
        }
    }



}