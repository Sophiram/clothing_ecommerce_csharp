using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly ISizeService _sizeService;
    private readonly IColorService _colorService;

    public ProductsController(
        IProductService productService,
        ISizeService sizeService,
        IColorService colorService)
    {
        _productService = productService;
        _sizeService = sizeService;
        _colorService = colorService;
    }

    // =========================================================
    // INDEX
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index(
        Guid? categoryId,
        Guid? brandId,
        string? search,
        ProductStatus? status)
    {
        var (products, _) = await _productService.GetAdminProductsAsync(categoryId, brandId, search, status);

        await LoadDropdownsAsync(categoryId, brandId);

        ViewBag.Search = search;
        ViewBag.Status = status;

        return View(products);
    }

    // =========================================================
    // DETAILS
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue)
        {
            TempData["Error"] = "Product ID is missing.";
            return RedirectToAction(nameof(Index));
        }

        var product = await _productService.GetProductDetailsAsync(id.Value);

        if (product == null)
        {
            TempData["Error"] = "Product not found.";
            return RedirectToAction(nameof(Index));
        }

        // Needed for the Add/Edit Variant modals
        ViewBag.AllSizes = await _sizeService.GetAllAsync();
        ViewBag.AllColors = await _colorService.GetAllAsync();
        ViewBag.StatusValues = Enum.GetValues(typeof(VariantStatus)).Cast<VariantStatus>().ToList();

        return View(product);
    }

    // =========================================================
    // CREATE - GET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();

        var product = new Product
        {
            Status = ProductStatus.Active
        };

        return View(product);
    }

    // =========================================================
    // CREATE - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        ModelState.Remove(nameof(Product.Category));
        ModelState.Remove(nameof(Product.Brand));
        ModelState.Remove(nameof(Product.Images));
        ModelState.Remove(nameof(Product.Variants));
        ModelState.Remove(nameof(Product.Reviews));

        ModelState.Remove(nameof(Product.Id));
        ModelState.Remove(nameof(Product.CreatedAt));
        ModelState.Remove(nameof(Product.ModifiedAt));

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync(product.CategoryId, product.BrandId);
            return View(product);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _productService.CreateProductAsync(product, userId, userEmail, ip);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to create the product.");
            TempData["Error"] = result.ErrorMessage ?? "Unable to create the product.";
            await LoadDropdownsAsync(product.CategoryId, product.BrandId);
            return View(product);
        }

        TempData["Success"] = $"Product '{product.Name}' was created successfully.";
        return RedirectToAction(nameof(Details), new { id = product.Id });
    }

    // =========================================================
    // EDIT - GET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue)
        {
            TempData["Error"] = "Product ID is missing.";
            return RedirectToAction(nameof(Index));
        }

        var product = await _productService.GetProductByIdAsync(id.Value);

        if (product == null)
        {
            TempData["Error"] = "Product not found.";
            return RedirectToAction(nameof(Index));
        }

        await LoadDropdownsAsync(product.CategoryId, product.BrandId);
        return View(product);
    }

    // =========================================================
    // EDIT - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Product product)
    {
        if (id != product.Id)
        {
            TempData["Error"] = "Invalid product ID.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.Remove(nameof(Product.Category));
        ModelState.Remove(nameof(Product.Brand));
        ModelState.Remove(nameof(Product.Images));
        ModelState.Remove(nameof(Product.Variants));
        ModelState.Remove(nameof(Product.Reviews));

        ModelState.Remove(nameof(Product.CreatedAt));
        ModelState.Remove(nameof(Product.ModifiedAt));

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync(product.CategoryId, product.BrandId);
            return View(product);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _productService.UpdateProductAsync(id, product, userId, userEmail, ip);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to update the product.");
            TempData["Error"] = result.ErrorMessage ?? "Unable to update the product.";
            await LoadDropdownsAsync(product.CategoryId, product.BrandId);
            return View(product);
        }

        TempData["Success"] = $"Product '{product.Name}' was updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // =========================================================
    // DELETE - GET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue)
        {
            TempData["Error"] = "Product ID is missing.";
            return RedirectToAction(nameof(Index));
        }

        var product = await _productService.GetProductDetailsAsync(id.Value);

        if (product == null)
        {
            TempData["Error"] = "Product not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(product);
    }

    // =========================================================
    // DELETE - POST
    // =========================================================

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _productService.DeleteProductAsync(id, userId, userEmail, ip);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage ?? "An unexpected error occurred while deleting the product.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = result.SuccessMessage ?? "Product deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    // =========================================================
    // DROPDOWNS
    // =========================================================

    private async Task LoadDropdownsAsync(Guid? categoryId = null, Guid? brandId = null)
    {
        var (categories, brands) = await _productService.GetCategoriesAndBrandsAsync();

        ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
        ViewBag.Brands = new SelectList(brands, "Id", "Name", brandId);
    }
}