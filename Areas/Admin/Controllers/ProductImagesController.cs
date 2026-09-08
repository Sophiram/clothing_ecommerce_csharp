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

        private bool IsValidImageUrl(string? url, out string? error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(url))
            {
                error = "Image URL is required.";
                return false;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                error = "Please enter a valid image URL.";
                return false;
            }

            return true;
        }

        // =====================================================
        // CREATE - POST (AJAX)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductImage image)
        {
            ModelState.Remove(nameof(ProductImage.Product));

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == image.ProductId);

            if (product == null)
            {
                return NotFound(new { success = false, errors = new[] { "Product not found." } });
            }

            if (!IsValidImageUrl(image.ImageUrl, out var urlError))
            {
                return BadRequest(new { success = false, errors = new[] { urlError } });
            }

            var existingImages = await _context.ProductImages
                .Where(i => i.ProductId == image.ProductId)
                .ToListAsync();

            image.Id = Guid.NewGuid();

            if (!existingImages.Any())
            {
                image.IsPrimary = true;
            }

            if (image.IsPrimary)
            {
                foreach (var existingImage in existingImages)
                {
                    existingImage.IsPrimary = false;
                }
            }

            _context.ProductImages.Add(image);
            product.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Product image added successfully." });
        }

        // =====================================================
        // EDIT - POST (AJAX)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Guid productId, string imageUrl, bool isPrimary)
        {
            var image = await _context.ProductImages
                .FirstOrDefaultAsync(i => i.Id == id && i.ProductId == productId);

            if (image == null)
            {
                return NotFound(new { success = false, errors = new[] { "Image not found." } });
            }

            if (!IsValidImageUrl(imageUrl, out var urlError))
            {
                return BadRequest(new { success = false, errors = new[] { urlError } });
            }

            image.ImageUrl = imageUrl;

            if (isPrimary && !image.IsPrimary)
            {
                var siblings = await _context.ProductImages
                    .Where(i => i.ProductId == productId && i.Id != id)
                    .ToListAsync();

                foreach (var sibling in siblings)
                {
                    sibling.IsPrimary = false;
                }

                image.IsPrimary = true;
            }
            else if (!isPrimary && image.IsPrimary)
            {
                // Don't allow un-setting primary with nothing else to fall back on
                var otherCount = await _context.ProductImages
                    .CountAsync(i => i.ProductId == productId && i.Id != id);

                if (otherCount > 0)
                {
                    image.IsPrimary = false;

                    var newPrimary = await _context.ProductImages
                        .Where(i => i.ProductId == productId && i.Id != id)
                        .OrderBy(i => i.Id)
                        .FirstOrDefaultAsync();

                    if (newPrimary != null)
                    {
                        newPrimary.IsPrimary = true;
                    }
                }
                // if it's the only image, keep it primary regardless
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product != null)
            {
                product.ModifiedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Product image updated successfully." });
        }

        // =====================================================
        // SET PRIMARY - POST (AJAX)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimary(Guid id, Guid productId)
        {
            var selectedImage = await _context.ProductImages
                .FirstOrDefaultAsync(i => i.Id == id && i.ProductId == productId);

            if (selectedImage == null)
            {
                return NotFound(new { success = false, errors = new[] { "Image not found." } });
            }

            var images = await _context.ProductImages
                .Where(i => i.ProductId == productId)
                .ToListAsync();

            foreach (var image in images)
            {
                image.IsPrimary = false;
            }

            selectedImage.IsPrimary = true;

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product != null)
            {
                product.ModifiedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Primary image updated successfully." });
        }

        // =====================================================
        // DELETE - POST (AJAX)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, Guid productId)
        {
            var image = await _context.ProductImages
                .FirstOrDefaultAsync(i => i.Id == id && i.ProductId == productId);

            if (image == null)
            {
                return NotFound(new { success = false, errors = new[] { "Image not found." } });
            }

            bool wasPrimary = image.IsPrimary;

            _context.ProductImages.Remove(image);

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product != null)
            {
                product.ModifiedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            if (wasPrimary)
            {
                var newPrimary = await _context.ProductImages
                    .Where(i => i.ProductId == productId)
                    .OrderBy(i => i.Id)
                    .FirstOrDefaultAsync();

                if (newPrimary != null)
                {
                    newPrimary.IsPrimary = true;
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { success = true, message = "Product image deleted successfully." });
        }
    }
}