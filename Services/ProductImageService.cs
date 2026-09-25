using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class ProductImageService : IProductImageService
    {
        private readonly AppDbContext _context;

        public ProductImageService(AppDbContext context)
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

        public async Task<ServiceResult> AddImageAsync(ProductImage image)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == image.ProductId);
            if (product == null)
            {
                return ServiceResult.Fail("Product not found.");
            }

            if (!IsValidImageUrl(image.ImageUrl, out var urlError))
            {
                return ServiceResult.Fail(urlError!);
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

            return ServiceResult.Ok("Product image added successfully.");
        }

        public async Task<ServiceResult> UpdateImageAsync(Guid id, Guid productId, string imageUrl, bool isPrimary)
        {
            var image = await _context.ProductImages
                .FirstOrDefaultAsync(i => i.Id == id && i.ProductId == productId);

            if (image == null)
            {
                return ServiceResult.Fail("Image not found.");
            }

            if (!IsValidImageUrl(imageUrl, out var urlError))
            {
                return ServiceResult.Fail(urlError!);
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
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product != null)
            {
                product.ModifiedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Product image updated successfully.");
        }

        public async Task<ServiceResult> SetPrimaryImageAsync(Guid id, Guid productId)
        {
            var selectedImage = await _context.ProductImages
                .FirstOrDefaultAsync(i => i.Id == id && i.ProductId == productId);

            if (selectedImage == null)
            {
                return ServiceResult.Fail("Image not found.");
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

            return ServiceResult.Ok("Primary image updated successfully.");
        }

        public async Task<ServiceResult> DeleteImageAsync(Guid id, Guid productId)
        {
            var image = await _context.ProductImages
                .FirstOrDefaultAsync(i => i.Id == id && i.ProductId == productId);

            if (image == null)
            {
                return ServiceResult.Fail("Image not found.");
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

            return ServiceResult.Ok("Product image deleted successfully.");
        }
    }
}
