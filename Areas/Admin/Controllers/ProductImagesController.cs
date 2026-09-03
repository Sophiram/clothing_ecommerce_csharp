using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductImagesController : Controller
    {
        private readonly AppDbContext _context;

        public ProductImagesController(AppDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // CREATE - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Create(Guid productId)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                TempData["Error"] = "Product not found.";

                return RedirectToAction(
                    "Index",
                    "Products",
                    new { area = "Admin" });
            }

            ViewBag.Product = product;

            var image = new ProductImage
            {
                ProductId = productId
            };

            return View(image);
        }


        // =====================================================
        // CREATE - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductImage image)
        {
            // IMPORTANT:
            // Product is navigation property.
            // It is NOT submitted by the form.
            ModelState.Remove(nameof(ProductImage.Product));

            // -------------------------------------------------
            // FIND PRODUCT
            // -------------------------------------------------

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == image.ProductId);

            if (product == null)
            {
                TempData["Error"] = "Product not found.";

                return RedirectToAction(
                    "Index",
                    "Products",
                    new { area = "Admin" });
            }

            // -------------------------------------------------
            // IMAGE URL VALIDATION
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(image.ImageUrl))
            {
                ModelState.AddModelError(
                    nameof(ProductImage.ImageUrl),
                    "Image URL is required.");
            }

            // -------------------------------------------------
            // VALIDATE URL FORMAT
            // -------------------------------------------------

            if (!string.IsNullOrWhiteSpace(image.ImageUrl))
            {
                if (!Uri.TryCreate(
                        image.ImageUrl,
                        UriKind.Absolute,
                        out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp &&
                     uri.Scheme != Uri.UriSchemeHttps))
                {
                    ModelState.AddModelError(
                        nameof(ProductImage.ImageUrl),
                        "Please enter a valid image URL.");
                }
            }

            // -------------------------------------------------
            // MODEL VALIDATION
            // -------------------------------------------------

            if (!ModelState.IsValid)
            {
                ViewBag.Product = product;

                return View(image);
            }

            // -------------------------------------------------
            // GET EXISTING IMAGES
            // -------------------------------------------------

            var existingImages = await _context.ProductImages
                .Where(i =>
                    i.ProductId == image.ProductId)
                .ToListAsync();

            // -------------------------------------------------
            // GENERATE ID
            // -------------------------------------------------

            image.Id = Guid.NewGuid();

            // -------------------------------------------------
            // FIRST IMAGE
            // -------------------------------------------------

            if (!existingImages.Any())
            {
                image.IsPrimary = true;
            }

            // -------------------------------------------------
            // SET PRIMARY
            // -------------------------------------------------

            if (image.IsPrimary)
            {
                foreach (var existingImage in existingImages)
                {
                    existingImage.IsPrimary = false;
                }
            }

            // -------------------------------------------------
            // ADD IMAGE
            // -------------------------------------------------

            _context.ProductImages.Add(image);

            // -------------------------------------------------
            // UPDATE PRODUCT
            // -------------------------------------------------

            product.ModifiedAt = DateTime.UtcNow;

            // -------------------------------------------------
            // SAVE
            // -------------------------------------------------

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product image added successfully.";

            return RedirectToAction(
                "Details",
                "Products",
                new
                {
                    area = "Admin",
                    id = image.ProductId
                });
        }


        // =====================================================
        // SET PRIMARY
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimary(
            Guid id,
            Guid productId)
        {
            var selectedImage =
                await _context.ProductImages
                    .FirstOrDefaultAsync(i =>
                        i.Id == id &&
                        i.ProductId == productId);

            if (selectedImage == null)
            {
                TempData["Error"] =
                    "Image not found.";

                return RedirectToAction(
                    "Details",
                    "Products",
                    new
                    {
                        area = "Admin",
                        id = productId
                    });
            }

            var images =
                await _context.ProductImages
                    .Where(i =>
                        i.ProductId == productId)
                    .ToListAsync();

            foreach (var image in images)
            {
                image.IsPrimary = false;
            }

            selectedImage.IsPrimary = true;

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
                "Primary image updated successfully.";

            return RedirectToAction(
                "Details",
                "Products",
                new
                {
                    area = "Admin",
                    id = productId
                });
        }


        // =====================================================
        // DELETE
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            Guid id,
            Guid productId)
        {
            var image =
                await _context.ProductImages
                    .FirstOrDefaultAsync(i =>
                        i.Id == id &&
                        i.ProductId == productId);

            if (image == null)
            {
                TempData["Error"] =
                    "Image not found.";

                return RedirectToAction(
                    "Details",
                    "Products",
                    new
                    {
                        area = "Admin",
                        id = productId
                    });
            }

            bool wasPrimary =
                image.IsPrimary;

            _context.ProductImages.Remove(image);

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

            // -------------------------------------------------
            // SELECT NEW PRIMARY
            // -------------------------------------------------

            if (wasPrimary)
            {
                var newPrimary =
                    await _context.ProductImages
                        .Where(i =>
                            i.ProductId == productId)
                        .OrderBy(i => i.Id)
                        .FirstOrDefaultAsync();

                if (newPrimary != null)
                {
                    newPrimary.IsPrimary = true;

                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] =
                "Product image deleted successfully.";

            return RedirectToAction(
                "Details",
                "Products",
                new
                {
                    area = "Admin",
                    id = productId
                });
        }
    }
}