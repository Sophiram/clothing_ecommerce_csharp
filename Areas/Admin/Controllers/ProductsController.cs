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
public class ProductsController : Controller
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
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
        IQueryable<Product> products = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Include(p => p.Variants);

        if (categoryId.HasValue)
        {
            products = products.Where(p =>
                p.CategoryId == categoryId.Value);
        }

        if (brandId.HasValue)
        {
            products = products.Where(p =>
                p.BrandId == brandId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            products = products.Where(p =>
                p.Name.Contains(search) ||
                (p.Description != null &&
                 p.Description.Contains(search)));
        }

        if (status.HasValue)
        {
            products = products.Where(p =>
                p.Status == status.Value);
        }

        await LoadDropdownsAsync(
            categoryId,
            brandId);

        ViewBag.Search = search;
        ViewBag.Status = status;

        var result = await products
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(result);
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

        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Include(p => p.Variants).ThenInclude(v => v.Color)
            .Include(p => p.Variants).ThenInclude(v => v.Size)
            .Include(p => p.Variants).ThenInclude(v => v.Inventory)
            .Include(p => p.Reviews).ThenInclude(r => r.Customer)
            .FirstOrDefaultAsync(p => p.Id == id.Value);

        if (product == null)
        {
            TempData["Error"] = "Product not found.";
            return RedirectToAction(nameof(Index));
        }

        // Needed for the Add/Edit Variant modals
        ViewBag.AllSizes = await _context.Sizes.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
        ViewBag.AllColors = await _context.Colors.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
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
        // Remove navigation validation
        ModelState.Remove(nameof(Product.Category));
        ModelState.Remove(nameof(Product.Brand));
        ModelState.Remove(nameof(Product.Images));
        ModelState.Remove(nameof(Product.Variants));
        ModelState.Remove(nameof(Product.Reviews));

        // Server generated
        ModelState.Remove(nameof(Product.Id));
        ModelState.Remove(nameof(Product.CreatedAt));
        ModelState.Remove(nameof(Product.ModifiedAt));

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync(
                product.CategoryId,
                product.BrandId);

            return View(product);
        }

        try
        {
            product.Id = Guid.NewGuid();

            product.CreatedAt =
                DateTime.UtcNow;

            product.ModifiedAt =
                DateTime.UtcNow;

            if (!Enum.IsDefined(
                    typeof(ProductStatus),
                    product.Status))
            {
                product.Status =
                    ProductStatus.Active;
            }

            _context.Products.Add(product);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Product '{product.Name}' was created successfully.";

            // IMPORTANT:
            // Go directly to Details so user can
            // add images and variants.
            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = product.Id
                });
        }
        catch (DbUpdateException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.InnerException?.Message ??
                ex.Message);

            TempData["Error"] =
                "Unable to create the product.";

            await LoadDropdownsAsync(
                product.CategoryId,
                product.BrandId);

            return View(product);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            TempData["Error"] =
                "An unexpected error occurred.";

            await LoadDropdownsAsync(
                product.CategoryId,
                product.BrandId);

            return View(product);
        }
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
                "Product ID is missing.";

            return RedirectToAction(nameof(Index));
        }

        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Id == id.Value);

        if (product == null)
        {
            TempData["Error"] =
                "Product not found.";

            return RedirectToAction(nameof(Index));
        }

        await LoadDropdownsAsync(
            product.CategoryId,
            product.BrandId);

        return View(product);
    }


    // =========================================================
    // EDIT - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        Product product)
    {
        if (id != product.Id)
        {
            TempData["Error"] =
                "Invalid product ID.";

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
            await LoadDropdownsAsync(
                product.CategoryId,
                product.BrandId);

            return View(product);
        }

        try
        {
            var existingProduct =
                await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.Id == id);

            if (existingProduct == null)
            {
                TempData["Error"] =
                    "Product not found.";

                return RedirectToAction(nameof(Index));
            }

            existingProduct.Name =
                product.Name;

            existingProduct.Description =
                product.Description;

            existingProduct.CategoryId =
                product.CategoryId;

            existingProduct.BrandId =
                product.BrandId;

            existingProduct.Gender =
                product.Gender;

            existingProduct.Material =
                product.Material;

            existingProduct.Status =
                product.Status;

            existingProduct.ModifiedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Product '{existingProduct.Name}' was updated successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] =
                "The product was modified by another user.";

            await LoadDropdownsAsync(
                product.CategoryId,
                product.BrandId);

            return View(product);
        }
        catch (DbUpdateException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.InnerException?.Message ??
                ex.Message);

            TempData["Error"] =
                "Unable to update the product.";

            await LoadDropdownsAsync(
                product.CategoryId,
                product.BrandId);

            return View(product);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            TempData["Error"] =
                "An unexpected error occurred.";

            await LoadDropdownsAsync(
                product.CategoryId,
                product.BrandId);

            return View(product);
        }
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
                "Product ID is missing.";

            return RedirectToAction(nameof(Index));
        }

        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p =>
                p.Id == id.Value);

        if (product == null)
        {
            TempData["Error"] =
                "Product not found.";

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
    public async Task<IActionResult> DeleteConfirmed(
        Guid id)
    {
        try
        {
            var product =
                await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.Id == id);

            if (product == null)
            {
                TempData["Error"] =
                    "Product not found.";

                return RedirectToAction(nameof(Index));
            }

            var productName =
                product.Name;

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Product '{productName}' was deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            TempData["Error"] =
                "Cannot delete this product because it is being used by other records.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            TempData["Error"] =
                "An unexpected error occurred while deleting the product.";

            return RedirectToAction(nameof(Index));
        }
    }


    // =========================================================
    // DROPDOWNS
    // =========================================================

    private async Task LoadDropdownsAsync(
        Guid? categoryId = null,
        Guid? brandId = null)
    {
        var categories =
            await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

        var brands =
            await _context.Brands
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .ToListAsync();

        ViewBag.Categories =
            new SelectList(
                categories,
                "Id",
                "Name",
                categoryId);

        ViewBag.Brands =
            new SelectList(
                brands,
                "Id",
                "Name",
                brandId);
    }
}