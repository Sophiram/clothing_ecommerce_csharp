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
            query = query.Where(v =>
                v.ProductId == productId.Value);
        }

        var variants = await query
            .OrderBy(v => v.Product.Name)
            .ThenBy(v => v.Size.Name)
            .ThenBy(v => v.Color.Name)
            .ToListAsync();

        ViewBag.Products = new SelectList(
            await _context.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync(),
            "Id",
            "Name",
            productId);

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
            .Include(v => v.Product)
                .ThenInclude(p => p.Images)
            .Include(v => v.Size)
            .Include(v => v.Color)
            .Include(v => v.Inventory)
            .FirstOrDefaultAsync(v => v.Id == id.Value);

        if (variant == null)
        {
            TempData["Error"] = "Product variant not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(variant);
    }

    // =========================================================
    // CREATE - GET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Create(Guid? productId)
    {
        await LoadDropdownsAsync(productId);

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = productId ?? Guid.Empty,
            Status = VariantStatus.Available,
            Price = 0
        };

        ViewBag.Quantity = 0;

        return View(variant);
    }

    // =========================================================
    // CREATE - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ProductVariant variant,
        int quantity = 0)
    {
        // -----------------------------------------------------
        // Remove navigation-property validation
        // -----------------------------------------------------

        ModelState.Remove(nameof(ProductVariant.Product));
        ModelState.Remove(nameof(ProductVariant.Size));
        ModelState.Remove(nameof(ProductVariant.Color));
        ModelState.Remove(nameof(ProductVariant.Inventory));

        ModelState.Remove(nameof(ProductVariant.Id));

        // -----------------------------------------------------
        // Validate Product
        // -----------------------------------------------------

        if (variant.ProductId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(ProductVariant.ProductId),
                "Please select a product.");
        }
        else
        {
            var productExists =
                await _context.Products.AnyAsync(
                    p => p.Id == variant.ProductId);

            if (!productExists)
            {
                ModelState.AddModelError(
                    nameof(ProductVariant.ProductId),
                    "Selected product does not exist.");
            }
        }

        // -----------------------------------------------------
        // Validate Size
        // -----------------------------------------------------

        if (variant.SizeId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(ProductVariant.SizeId),
                "Please select a size.");
        }
        else
        {
            var sizeExists =
                await _context.Sizes.AnyAsync(
                    s => s.Id == variant.SizeId);

            if (!sizeExists)
            {
                ModelState.AddModelError(
                    nameof(ProductVariant.SizeId),
                    "Selected size does not exist.");
            }
        }

        // -----------------------------------------------------
        // Validate Color
        // -----------------------------------------------------

        if (variant.ColorId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(ProductVariant.ColorId),
                "Please select a color.");
        }
        else
        {
            var colorExists =
                await _context.Colors.AnyAsync(
                    c => c.Id == variant.ColorId);

            if (!colorExists)
            {
                ModelState.AddModelError(
                    nameof(ProductVariant.ColorId),
                    "Selected color does not exist.");
            }
        }

        // -----------------------------------------------------
        // Validate SKU
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(variant.SKU))
        {
            ModelState.AddModelError(
                nameof(ProductVariant.SKU),
                "SKU is required.");
        }

        // -----------------------------------------------------
        // Validate Price
        // -----------------------------------------------------

        if (variant.Price < 0)
        {
            ModelState.AddModelError(
                nameof(ProductVariant.Price),
                "Price cannot be negative.");
        }

        // -----------------------------------------------------
        // Validate Quantity
        // -----------------------------------------------------

        if (quantity < 0)
        {
            ModelState.AddModelError(
                "quantity",
                "Inventory quantity cannot be negative.");
        }

        // -----------------------------------------------------
        // Check duplicate Size + Color
        // -----------------------------------------------------

        if (variant.ProductId != Guid.Empty &&
            variant.SizeId != Guid.Empty &&
            variant.ColorId != Guid.Empty)
        {
            var duplicate =
                await _context.ProductVariants.AnyAsync(v =>
                    v.ProductId == variant.ProductId &&
                    v.SizeId == variant.SizeId &&
                    v.ColorId == variant.ColorId);

            if (duplicate)
            {
                ModelState.AddModelError(
                    "",
                    "This product already has this Size + Color variant.");
            }
        }

        // -----------------------------------------------------
        // Return View if invalid
        // -----------------------------------------------------

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync(
                variant.ProductId,
                variant.SizeId,
                variant.ColorId);

            ViewBag.Quantity = quantity;

            return View(variant);
        }

        // -----------------------------------------------------
        // Create Variant
        // -----------------------------------------------------

        variant.Id = Guid.NewGuid();

        variant.SKU = variant.SKU.Trim();

        _context.ProductVariants.Add(variant);

        // -----------------------------------------------------
        // Create Inventory
        // -----------------------------------------------------

        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductVariantId = variant.Id,
            Quantity = quantity,
            ReservedQuantity = 0,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Inventories.Add(inventory);

        // -----------------------------------------------------
        // Update Product
        // -----------------------------------------------------

        var product = await _context.Products
            .FirstOrDefaultAsync(p =>
                p.Id == variant.ProductId);

        if (product != null)
        {
            product.ModifiedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Variant '{variant.SKU}' was created successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id = variant.Id });
    }

    // =========================================================
    // EDIT - GET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue)
        {
            TempData["Error"] = "Variant ID is missing.";
            return RedirectToAction(nameof(Index));
        }

        var variant = await _context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Inventory)
            .FirstOrDefaultAsync(v =>
                v.Id == id.Value);

        if (variant == null)
        {
            TempData["Error"] = "Product variant not found.";
            return RedirectToAction(nameof(Index));
        }

        await LoadDropdownsAsync(
            variant.ProductId,
            variant.SizeId,
            variant.ColorId);

        ViewBag.Quantity =
            variant.Inventory?.Quantity ?? 0;

        return View(variant);
    }

    // =========================================================
    // EDIT - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        ProductVariant variant,
        int quantity = 0)
    {
        if (id != variant.Id)
        {
            TempData["Error"] = "Invalid variant ID.";
            return RedirectToAction(nameof(Index));
        }

        // -----------------------------------------------------
        // Remove navigation validation
        // THIS FIXES:
        // "The Product field is required"
        // "The Size field is required"
        // "The Color field is required"
        // -----------------------------------------------------

        ModelState.Remove(nameof(ProductVariant.Product));
        ModelState.Remove(nameof(ProductVariant.Size));
        ModelState.Remove(nameof(ProductVariant.Color));
        ModelState.Remove(nameof(ProductVariant.Inventory));

        // -----------------------------------------------------
        // Validate
        // -----------------------------------------------------

        if (variant.ProductId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(ProductVariant.ProductId),
                "Please select a product.");
        }

        if (variant.SizeId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(ProductVariant.SizeId),
                "Please select a size.");
        }

        if (variant.ColorId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(ProductVariant.ColorId),
                "Please select a color.");
        }

        if (string.IsNullOrWhiteSpace(variant.SKU))
        {
            ModelState.AddModelError(
                nameof(ProductVariant.SKU),
                "SKU is required.");
        }

        if (variant.Price < 0)
        {
            ModelState.AddModelError(
                nameof(ProductVariant.Price),
                "Price cannot be negative.");
        }

        if (quantity < 0)
        {
            ModelState.AddModelError(
                "quantity",
                "Inventory quantity cannot be negative.");
        }

        // -----------------------------------------------------
        // Duplicate check
        // -----------------------------------------------------

        var duplicate =
            await _context.ProductVariants.AnyAsync(v =>
                v.Id != id &&
                v.ProductId == variant.ProductId &&
                v.SizeId == variant.SizeId &&
                v.ColorId == variant.ColorId);

        if (duplicate)
        {
            ModelState.AddModelError(
                "",
                "This product already has this Size + Color variant.");
        }

        // -----------------------------------------------------
        // Return View if invalid
        // -----------------------------------------------------

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync(
                variant.ProductId,
                variant.SizeId,
                variant.ColorId);

            ViewBag.Quantity = quantity;

            return View(variant);
        }

        // -----------------------------------------------------
        // Find existing
        // -----------------------------------------------------

        var existing =
            await _context.ProductVariants
                .Include(v => v.Inventory)
                .FirstOrDefaultAsync(v =>
                    v.Id == id);

        if (existing == null)
        {
            TempData["Error"] =
                "Product variant not found.";

            return RedirectToAction(nameof(Index));
        }

        // -----------------------------------------------------
        // Update Variant
        // -----------------------------------------------------

        var oldProductId = existing.ProductId;

        existing.ProductId = variant.ProductId;
        existing.SizeId = variant.SizeId;
        existing.ColorId = variant.ColorId;
        existing.SKU = variant.SKU.Trim();
        existing.Price = variant.Price;
        existing.Status = variant.Status;

        // -----------------------------------------------------
        // Update Inventory
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // Update Product ModifiedAt
        // -----------------------------------------------------

        var productIds = new[]
        {
            oldProductId,
            existing.ProductId
        };

        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        foreach (var product in products)
        {
            product.ModifiedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Variant '{existing.SKU}' was updated successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id = existing.Id });
    }

    // =========================================================
    // DELETE - GET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue)
        {
            TempData["Error"] = "Variant ID is missing.";
            return RedirectToAction(nameof(Index));
        }

        var variant = await _context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
                .ThenInclude(p => p.Images)
            .Include(v => v.Size)
            .Include(v => v.Color)
            .Include(v => v.Inventory)
            .FirstOrDefaultAsync(v =>
                v.Id == id.Value);

        if (variant == null)
        {
            TempData["Error"] =
                "Product variant not found.";

            return RedirectToAction(nameof(Index));
        }

        return View(variant);
    }

    // =========================================================
    // DELETE - POST
    // =========================================================

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var variant = await _context.ProductVariants
            .Include(v => v.Inventory)
            .FirstOrDefaultAsync(v =>
                v.Id == id);

        if (variant == null)
        {
            TempData["Error"] =
                "Product variant not found.";

            return RedirectToAction(nameof(Index));
        }

        var productId = variant.ProductId;
        var sku = variant.SKU;

        try
        {
            // Inventory relationship is 1:1.
            // Explicitly remove inventory first.
            if (variant.Inventory != null)
            {
                _context.Inventories.Remove(
                    variant.Inventory);
            }

            _context.ProductVariants.Remove(variant);

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == productId);

            if (product != null)
            {
                product.ModifiedAt =
                    DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Variant '{sku}' was deleted successfully.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] =
                "Cannot delete this variant because it is being used by other records.";
        }
        catch (Exception)
        {
            TempData["Error"] =
                "An unexpected error occurred while deleting the variant.";
        }

        return RedirectToAction(
            nameof(Index),
            new { productId });
    }

    // =========================================================
    // DROPDOWNS
    // =========================================================

    private async Task LoadDropdownsAsync(
        Guid? productId = null,
        Guid? sizeId = null,
        Guid? colorId = null)
    {
        ViewBag.Products = new SelectList(
            await _context.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync(),
            "Id",
            "Name",
            productId);

        ViewBag.Sizes = new SelectList(
            await _context.Sizes
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync(),
            "Id",
            "Name",
            sizeId);

        ViewBag.Colors = new SelectList(
            await _context.Colors
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync(),
            "Id",
            "Name",
            colorId);
    }
}