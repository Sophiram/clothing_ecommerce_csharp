using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class InventoriesController : Controller
{
    private readonly IInventoryService _inventoryService;

    public InventoriesController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    // =========================================================
    // INDEX
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index(Guid? productId)
    {
        var inventories = await _inventoryService.GetInventoriesAsync(productId);
        var products = await _inventoryService.GetAllProductsAsync();

        ViewBag.Products = new SelectList(products, "Id", "Name", productId);
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
            TempData["Error"] = "Inventory ID is missing.";
            return RedirectToAction(nameof(Index));
        }

        var inventory = await _inventoryService.GetInventoryDetailsAsync(id.Value);

        if (inventory == null)
        {
            TempData["Error"] = "Inventory record not found.";
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
        await LoadVariantDropdownAsync(productVariantId);

        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductVariantId = productVariantId ?? Guid.Empty,
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
    public async Task<IActionResult> Create(Inventory inventory)
    {
        ModelState.Remove(nameof(Inventory.ProductVariant));
        ModelState.Remove(nameof(Inventory.Id));
        ModelState.Remove(nameof(Inventory.UpdatedAt));

        if (!ModelState.IsValid)
        {
            await LoadVariantDropdownAsync(inventory.ProductVariantId);
            return View(inventory);
        }

        var result = await _inventoryService.CreateInventoryAsync(inventory);

        if (!result.Success)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err);
            }
            await LoadVariantDropdownAsync(inventory.ProductVariantId);
            return View(inventory);
        }

        TempData["Success"] = result.SuccessMessage ?? "Inventory was created successfully.";
        return RedirectToAction(nameof(Details), new { id = inventory.Id });
    }

    // =========================================================
    // EDIT - GET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue)
        {
            TempData["Error"] = "Inventory ID is missing.";
            return RedirectToAction(nameof(Index));
        }

        var inventory = await _inventoryService.GetInventoryDetailsAsync(id.Value);

        if (inventory == null)
        {
            TempData["Error"] = "Inventory record not found.";
            return RedirectToAction(nameof(Index));
        }

        await LoadVariantDropdownAsync(inventory.ProductVariantId);
        return View(inventory);
    }

    // =========================================================
    // EDIT - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Inventory inventory)
    {
        if (id != inventory.Id)
        {
            TempData["Error"] = "Invalid inventory ID.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.Remove(nameof(Inventory.ProductVariant));
        ModelState.Remove(nameof(Inventory.UpdatedAt));

        if (!ModelState.IsValid)
        {
            await LoadVariantDropdownAsync(inventory.ProductVariantId);
            return View(inventory);
        }

        var result = await _inventoryService.UpdateInventoryAsync(id, inventory);

        if (!result.Success)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err);
            }
            await LoadVariantDropdownAsync(inventory.ProductVariantId);
            return View(inventory);
        }

        TempData["Success"] = result.SuccessMessage ?? "Inventory was updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // =========================================================
    // DELETE - GET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue)
        {
            TempData["Error"] = "Inventory ID is missing.";
            return RedirectToAction(nameof(Index));
        }

        var inventory = await _inventoryService.GetInventoryDetailsAsync(id.Value);

        if (inventory == null)
        {
            TempData["Error"] = "Inventory record not found.";
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
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var result = await _inventoryService.DeleteInventoryAsync(id);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage ?? "An unexpected error occurred while deleting inventory.";
        }
        else
        {
            TempData["Success"] = result.SuccessMessage ?? "Inventory was deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    // =========================================================
    // DROPDOWN
    // =========================================================

    private async Task LoadVariantDropdownAsync(Guid? selectedId = null)
    {
        var items = await _inventoryService.GetVariantDropdownItemsAsync();
        ViewBag.ProductVariants = new SelectList(items, "Id", "Name", selectedId);
    }
}