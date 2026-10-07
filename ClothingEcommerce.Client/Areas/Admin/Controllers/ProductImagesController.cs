using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ProductImagesController : Controller
    {
        private readonly IProductImageService _imageService;
        private readonly IWebHostEnvironment _environment;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public ProductImagesController(IProductImageService imageService, IWebHostEnvironment environment)
        {
            _imageService = imageService;
            _environment = environment;
        }

        // =====================================================
        // CREATE - POST (AJAX)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductImage image, IFormFile? imageFile, string? imageUrl)
        {
            ModelState.Remove(nameof(ProductImage.Product));
            ModelState.Remove(nameof(ProductImage.ImageUrl));

            // Resolve the image URL: prefer uploaded file, then pasted URL
            if (imageFile != null && imageFile.Length > 0)
            {
                var fileError = ValidateImageFile(imageFile);
                if (fileError != null)
                    return BadRequest(new { success = false, errors = new[] { fileError } });

                var savedPath = await SaveProductImageAsync(imageFile);
                image.ImageUrl = savedPath;
            }
            else if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                if (!Uri.TryCreate(imageUrl.Trim(), UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    return BadRequest(new { success = false, errors = new[] { "Please enter a valid image URL (must start with http:// or https://)." } });
                }
                image.ImageUrl = imageUrl.Trim();
            }
            else
            {
                return BadRequest(new { success = false, errors = new[] { "Please upload an image file or paste an image URL." } });
            }

            var result = await _imageService.AddImageAsync(image);

            if (!result.Success)
            {
                if (result.ErrorMessage == "Product not found.")
                {
                    return NotFound(new { success = false, errors = new[] { result.ErrorMessage } });
                }
                return BadRequest(new { success = false, errors = result.Errors });
            }

            return Ok(new { success = true, message = result.SuccessMessage });
        }

        // =====================================================
        // EDIT - POST (AJAX)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Guid productId, string imageUrl, bool isPrimary)
        {
            var result = await _imageService.UpdateImageAsync(id, productId, imageUrl, isPrimary);

            if (!result.Success)
            {
                if (result.ErrorMessage == "Image not found.")
                {
                    return NotFound(new { success = false, errors = new[] { result.ErrorMessage } });
                }
                return BadRequest(new { success = false, errors = result.Errors });
            }

            return Ok(new { success = true, message = result.SuccessMessage });
        }

        // =====================================================
        // SET PRIMARY - POST (AJAX)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimary(Guid id, Guid productId)
        {
            var result = await _imageService.SetPrimaryImageAsync(id, productId);

            if (!result.Success)
            {
                return NotFound(new { success = false, errors = new[] { result.ErrorMessage } });
            }

            return Ok(new { success = true, message = result.SuccessMessage });
        }

        // =====================================================
        // DELETE - POST (AJAX)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, Guid productId)
        {
            var result = await _imageService.DeleteImageAsync(id, productId);

            if (!result.Success)
            {
                return NotFound(new { success = false, errors = new[] { result.ErrorMessage } });
            }

            return Ok(new { success = true, message = result.SuccessMessage });
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static string? ValidateImageFile(IFormFile file)
        {
            if (file.Length > MaxFileSize)
                return "Image file must be smaller than 5 MB.";

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return "Only JPG, JPEG, PNG, WEBP and GIF images are allowed.";

            return null;
        }

        private async Task<string> SaveProductImageAsync(IFormFile file)
        {
            var folder = Path.Combine(_environment.WebRootPath, "images", "products");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(folder, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/images/products/{fileName}";
        }
    }
}