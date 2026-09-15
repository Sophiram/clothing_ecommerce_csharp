using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class ProductVariantsController : Controller
{
    private readonly IProductVariantService _variantService;

    public ProductVariantsController(IProductVariantService variantService)
    {
        _variantService = variantService;
    }

    // =========================================================
    // INDEX
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index(Guid? productId)
    {
        var variants = await _variantService.GetVariantsAsync(productId);
        var (products, sizes, colors) = await _variantService.GetVariantDropdownDataAsync();

        ViewBag.Products = new SelectList(products, "Id", "Name", productId);
        ViewBag.AllProducts = products;
        ViewBag.AllSizes = sizes;
        ViewBag.AllColors = colors;
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

        var variant = await _variantService.GetVariantDetailsAsync(id.Value);

        if (variant == null)
        {
            TempData["Error"] = "Product variant not found.";
            return RedirectToAction(nameof(Index));
        }

        var (products, sizes, colors) = await _variantService.GetVariantDropdownDataAsync();
        ViewBag.AllProducts = products;
        ViewBag.AllSizes = sizes;
        ViewBag.AllColors = colors;
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

        var result = await _variantService.CreateVariantAsync(variant, quantity);

        if (!result.Success)
        {
            return BadRequest(new { success = false, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.SuccessMessage });
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

        var result = await _variantService.UpdateVariantAsync(id, variant, quantity);

        if (!result.Success)
        {
            if (result.ErrorMessage == "Product variant not found.")
            {
                return NotFound(new { success = false, errors = new[] { result.ErrorMessage } });
            }
            return BadRequest(new { success = false, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.SuccessMessage });
    }

    // =========================================================
    // DELETE - POST (AJAX)
    // =========================================================

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var result = await _variantService.DeleteVariantAsync(id);

        if (!result.Success)
        {
            if (result.ErrorMessage == "Product variant not found.")
            {
                return NotFound(new { success = false, errors = new[] { result.ErrorMessage } });
            }
            return BadRequest(new { success = false, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.SuccessMessage });
    }
}