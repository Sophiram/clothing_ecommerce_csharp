using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class InventoriesController : Controller
{
    private readonly AppDbContext _context;

    public InventoriesController(AppDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // INDEX
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index(Guid? productId)
    {
        var query = _context.Inventories
            .AsNoTracking()
            .Include(i => i.ProductVariant)
                .ThenInclude(v => v.Product)
            .Include(i => i.ProductVariant)
                .ThenInclude(v => v.Size)
            .Include(i => i.ProductVariant)
                .ThenInclude(v => v.Color)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(i =>
                i.ProductVariant.ProductId == productId.Value);
        }

        var inventories = await query
            .OrderBy(i => i.ProductVariant.Product.Name)
            .ThenBy(i => i.ProductVariant.Size.Name)
            .ThenBy(i => i.ProductVariant.Color.Name)
            .ToListAsync();

        ViewBag.Products = new SelectList(
            await _context.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync(),
            "Id",
            "Name",
            productId
        );

        ViewBag.SelectedProductId = productId;

        return View(inventories);
    }


    // =========================================================
    // DETAILS
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue)
        {
            TempData["Error"] =
                "Inventory ID is missing.";

            return RedirectToAction(nameof(Index));
        }

        var inventory = await _context.Inventories
            .AsNoTracking()

            .Include(i => i.ProductVariant)
                .ThenInclude(v => v.Product)
                    .ThenInclude(p => p.Images)

            .Include(i => i.ProductVariant)
                .ThenInclude(v => v.Size)

            .Include(i => i.ProductVariant)
                .ThenInclude(v => v.Color)

            .FirstOrDefaultAsync(i =>
                i.Id == id.Value);

        if (inventory == null)
        {
            TempData["Error"] =
                "Inventory record not found.";

            return RedirectToAction(nameof(Index));
        }

        return View(inventory);
    }


    // =========================================================
    // CREATE - GET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Create(Guid? productVariantId)
    {
        await LoadVariantDropdownAsync(
            productVariantId);

        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductVariantId =
                productVariantId ?? Guid.Empty,
            Quantity = 0,
            ReservedQuantity = 0,
            UpdatedAt = DateTime.UtcNow
        };

        return View(inventory);
    }


    // =========================================================
    // CREATE - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Inventory inventory)
    {
        // -----------------------------------------------------
        // Remove navigation validation
        // -----------------------------------------------------

        ModelState.Remove(
            nameof(Inventory.ProductVariant));

        ModelState.Remove(
            nameof(Inventory.Id));

        ModelState.Remove(
            nameof(Inventory.UpdatedAt));


        // -----------------------------------------------------
        // Validate Variant
        // -----------------------------------------------------

        if (inventory.ProductVariantId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(inventory.ProductVariantId),
                "Please select a product variant.");
        }
        else
        {
            var variantExists =
                await _context.ProductVariants
                    .AnyAsync(v =>
                        v.Id == inventory.ProductVariantId);

            if (!variantExists)
            {
                ModelState.AddModelError(
                    nameof(inventory.ProductVariantId),
                    "Selected product variant does not exist.");
            }
        }


        // -----------------------------------------------------
        // Validate Quantity
        // -----------------------------------------------------

        if (inventory.Quantity < 0)
        {
            ModelState.AddModelError(
                nameof(inventory.Quantity),
                "Quantity cannot be negative.");
        }


        // -----------------------------------------------------
        // Validate Reserved Quantity
        // -----------------------------------------------------

        if (inventory.ReservedQuantity < 0)
        {
            ModelState.AddModelError(
                nameof(inventory.ReservedQuantity),
                "Reserved quantity cannot be negative.");
        }

        if (inventory.ReservedQuantity >
            inventory.Quantity)
        {
            ModelState.AddModelError(
                nameof(inventory.ReservedQuantity),
                "Reserved quantity cannot be greater than total quantity.");
        }


        // -----------------------------------------------------
        // Check duplicate inventory
        // -----------------------------------------------------

        if (inventory.ProductVariantId != Guid.Empty)
        {
            var exists =
                await _context.Inventories
                    .AnyAsync(i =>
                        i.ProductVariantId ==
                        inventory.ProductVariantId);

            if (exists)
            {
                ModelState.AddModelError(
                    "",
                    "This product variant already has an inventory record.");
            }
        }


        // -----------------------------------------------------
        // Return View if invalid
        // -----------------------------------------------------

        if (!ModelState.IsValid)
        {
            await LoadVariantDropdownAsync(
                inventory.ProductVariantId);

            return View(inventory);
        }


        // -----------------------------------------------------
        // Create
        // -----------------------------------------------------

        inventory.Id = Guid.NewGuid();

        inventory.UpdatedAt =
            DateTime.UtcNow;

        _context.Inventories.Add(inventory);

        // -----------------------------------------------------
        // Update Product ModifiedAt
        // -----------------------------------------------------

        var variant =
            await _context.ProductVariants
                .FirstOrDefaultAsync(v =>
                    v.Id ==
                    inventory.ProductVariantId);

        if (variant != null)
        {
            var product =
                await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.Id ==
                        variant.ProductId);

            if (product != null)
            {
                product.ModifiedAt =
                    DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Inventory was created successfully.";

        return RedirectToAction(
            nameof(Details),
            new
            {
                id = inventory.Id
            });
    }


    // =========================================================
    // EDIT - GET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue)
        {
            TempData["Error"] =
                "Inventory ID is missing.";

            return RedirectToAction(nameof(Index));
        }

        var inventory =
            await _context.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i =>
                    i.Id == id.Value);

        if (inventory == null)
        {
            TempData["Error"] =
                "Inventory record not found.";

            return RedirectToAction(nameof(Index));
        }

        await LoadVariantDropdownAsync(
            inventory.ProductVariantId);

        return View(inventory);
    }


    // =========================================================
    // EDIT - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        Inventory inventory)
    {
        if (id != inventory.Id)
        {
            TempData["Error"] =
                "Invalid inventory ID.";

            return RedirectToAction(nameof(Index));
        }


        // -----------------------------------------------------
        // Remove navigation validation
        // -----------------------------------------------------

        ModelState.Remove(
            nameof(Inventory.ProductVariant));

        ModelState.Remove(
            nameof(Inventory.UpdatedAt));


        // -----------------------------------------------------
        // Validate Quantity
        // -----------------------------------------------------

        if (inventory.Quantity < 0)
        {
            ModelState.AddModelError(
                nameof(inventory.Quantity),
                "Quantity cannot be negative.");
        }


        // -----------------------------------------------------
        // Validate Reserved
        // -----------------------------------------------------

        if (inventory.ReservedQuantity < 0)
        {
            ModelState.AddModelError(
                nameof(inventory.ReservedQuantity),
                "Reserved quantity cannot be negative.");
        }

        if (inventory.ReservedQuantity >
            inventory.Quantity)
        {
            ModelState.AddModelError(
                nameof(inventory.ReservedQuantity),
                "Reserved quantity cannot be greater than total quantity.");
        }


        // -----------------------------------------------------
        // Validate Product Variant
        // -----------------------------------------------------

        if (inventory.ProductVariantId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(inventory.ProductVariantId),
                "Please select a product variant.");
        }


        // -----------------------------------------------------
        // Duplicate check
        // -----------------------------------------------------

        var duplicate =
            await _context.Inventories
                .AnyAsync(i =>
                    i.Id != id &&
                    i.ProductVariantId ==
                    inventory.ProductVariantId);

        if (duplicate)
        {
            ModelState.AddModelError(
                nameof(inventory.ProductVariantId),
                "This product variant already has an inventory record.");
        }


        // -----------------------------------------------------
        // Return View if invalid
        // -----------------------------------------------------

        if (!ModelState.IsValid)
        {
            await LoadVariantDropdownAsync(
                inventory.ProductVariantId);

            return View(inventory);
        }


        // -----------------------------------------------------
        // Find existing
        // -----------------------------------------------------

        var existing =
            await _context.Inventories
                .FirstOrDefaultAsync(i =>
                    i.Id == id);

        if (existing == null)
        {
            TempData["Error"] =
                "Inventory record not found.";

            return RedirectToAction(nameof(Index));
        }


        var oldVariantId =
            existing.ProductVariantId;


        // -----------------------------------------------------
        // Update
        // -----------------------------------------------------

        existing.ProductVariantId =
            inventory.ProductVariantId;

        existing.Quantity =
            inventory.Quantity;

        existing.ReservedQuantity =
            inventory.ReservedQuantity;

        existing.UpdatedAt =
            DateTime.UtcNow;


        // -----------------------------------------------------
        // Update Product ModifiedAt
        // -----------------------------------------------------

        var variantIds = new[]
        {
            oldVariantId,
            existing.ProductVariantId
        };

        var variants =
            await _context.ProductVariants
                .Where(v =>
                    variantIds.Contains(v.Id))
                .ToListAsync();

        var productIds =
            variants
                .Select(v => v.ProductId)
                .Distinct()
                .ToList();

        var products =
            await _context.Products
                .Where(p =>
                    productIds.Contains(p.Id))
                .ToListAsync();

        foreach (var product in products)
        {
            product.ModifiedAt =
                DateTime.UtcNow;
        }


        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Inventory was updated successfully.";

        return RedirectToAction(
            nameof(Details),
            new
            {
                id = existing.Id
            });
    }


    // =========================================================
    // DELETE - GET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue)
        {
            TempData["Error"] =
                "Inventory ID is missing.";

            return RedirectToAction(nameof(Index));
        }

        var inventory =
            await _context.Inventories
                .AsNoTracking()

                .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)

                .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Size)

                .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Color)

                .FirstOrDefaultAsync(i =>
                    i.Id == id.Value);

        if (inventory == null)
        {
            TempData["Error"] =
                "Inventory record not found.";

            return RedirectToAction(nameof(Index));
        }

        return View(inventory);
    }


    // =========================================================
    // DELETE - POST
    // =========================================================

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        Guid id)
    {
        var inventory =
            await _context.Inventories
                .Include(i => i.ProductVariant)
                .FirstOrDefaultAsync(i =>
                    i.Id == id);

        if (inventory == null)
        {
            TempData["Error"] =
                "Inventory record not found.";

            return RedirectToAction(nameof(Index));
        }

        try
        {
            var productId =
                inventory.ProductVariant.ProductId;

            _context.Inventories.Remove(
                inventory);

            var product =
                await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.Id == productId);

            if (product != null)
            {
                product.ModifiedAt =
                    DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Inventory was deleted successfully.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] =
                "Cannot delete this inventory record because it is being used by another record.";
        }
        catch (Exception)
        {
            TempData["Error"] =
                "An unexpected error occurred while deleting inventory.";
        }

        return RedirectToAction(nameof(Index));
    }


    // =========================================================
    // DROPDOWN
    // =========================================================

    private async Task LoadVariantDropdownAsync(
        Guid? selectedId = null)
    {
        var variants =
            await _context.ProductVariants
                .AsNoTracking()
                .Include(v => v.Product)
                .Include(v => v.Size)
                .Include(v => v.Color)
                .OrderBy(v => v.Product.Name)
                .ThenBy(v => v.Size.Name)
                .ThenBy(v => v.Color.Name)
                .ToListAsync();

        var items = variants.Select(v => new
        {
            Id = v.Id,

            Name =
                $"{v.Product.Name} | " +
                $"Size: {v.Size.Name} | " +
                $"Color: {v.Color.Name} | " +
                $"SKU: {v.SKU}"
        });

        ViewBag.ProductVariants =
            new SelectList(
                items,
                "Id",
                "Name",
                selectedId);
    }
}