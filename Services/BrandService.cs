using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class BrandService : IBrandService
    {
        private readonly AppDbContext _context;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public BrandService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Brand>> GetBrandsAsync(string? search = null)
        {
            var query = _context.Brands
                .Include(b => b.Products)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(b => b.Name.Contains(s) || (b.Description != null && b.Description.Contains(s)));
            }

            return await query.OrderBy(b => b.Name).ToListAsync();
        }

        public async Task<Brand?> GetBrandDetailsAsync(Guid id)
        {
            return await _context.Brands
                .Include(b => b.Products).ThenInclude(p => p.Category)
                .Include(b => b.Products).ThenInclude(p => p.Variants)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<BrandResult> CreateBrandAsync(Brand brand, IFormFile? logo, string? logoUrl, string webRootPath)
        {
            if (logo != null && logo.Length > 0)
            {
                var validationError = ValidateLogo(logo);
                if (validationError != null)
                {
                    return new BrandResult { Success = false, Errors = new List<string> { validationError } };
                }

                brand.Logo = await SaveLogoAsync(logo, webRootPath);
            }
            else if (!string.IsNullOrWhiteSpace(logoUrl))
            {
                // Validate it's a proper URL
                if (Uri.TryCreate(logoUrl.Trim(), UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    brand.Logo = logoUrl.Trim();
                }
                else
                {
                    return new BrandResult { Success = false, Errors = new List<string> { "Please enter a valid image URL (must start with http:// or https://)." } };
                }
            }

            brand.Id = Guid.NewGuid();
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            return new BrandResult
            {
                Success = true,
                Message = $"Brand '{brand.Name}' was created successfully.",
                Brand = brand
            };
        }

        public async Task<BrandResult> UpdateBrandAsync(Guid id, Brand brand, IFormFile? logo, string? logoUrl, bool removeLogo, string webRootPath)
        {
            var existingBrand = await _context.Brands.FirstOrDefaultAsync(b => b.Id == id);
            if (existingBrand == null)
            {
                return new BrandResult { Success = false, Errors = new List<string> { "Brand not found." } };
            }

            if (logo != null && logo.Length > 0)
            {
                var validationError = ValidateLogo(logo);
                if (validationError != null)
                {
                    return new BrandResult { Success = false, Errors = new List<string> { validationError } };
                }

                var oldLogo = existingBrand.Logo;
                existingBrand.Logo = await SaveLogoAsync(logo, webRootPath);
                // Only delete old logo if it was a local file (not an external URL)
                if (!string.IsNullOrWhiteSpace(oldLogo) && oldLogo.StartsWith("/images/"))
                    DeleteLogo(oldLogo, webRootPath);
            }
            else if (!string.IsNullOrWhiteSpace(logoUrl))
            {
                if (Uri.TryCreate(logoUrl.Trim(), UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    // Delete old local file if replacing with URL
                    if (!string.IsNullOrWhiteSpace(existingBrand.Logo) && existingBrand.Logo.StartsWith("/images/"))
                        DeleteLogo(existingBrand.Logo, webRootPath);
                    existingBrand.Logo = logoUrl.Trim();
                }
                else
                {
                    return new BrandResult { Success = false, Errors = new List<string> { "Please enter a valid image URL (must start with http:// or https://)." } };
                }
            }
            else if (removeLogo && !string.IsNullOrWhiteSpace(existingBrand.Logo))
            {
                if (existingBrand.Logo.StartsWith("/images/"))
                    DeleteLogo(existingBrand.Logo, webRootPath);
                existingBrand.Logo = null;
            }

            existingBrand.Name = brand.Name;
            existingBrand.Description = brand.Description;

            await _context.SaveChangesAsync();

            return new BrandResult
            {
                Success = true,
                Message = $"Brand '{existingBrand.Name}' was updated successfully.",
                Brand = existingBrand
            };
        }

        public async Task<ServiceResult> DeleteBrandAsync(Guid id, string webRootPath)
        {
            var brand = await _context.Brands.FirstOrDefaultAsync(b => b.Id == id);
            if (brand == null)
            {
                return ServiceResult.Fail("Brand not found.");
            }

            var hasProducts = await _context.Products.AnyAsync(p => p.BrandId == id);
            if (hasProducts)
            {
                return ServiceResult.Fail("This brand cannot be deleted because it has products.");
            }

            var name = brand.Name;
            DeleteLogo(brand.Logo, webRootPath);
            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok($"Brand '{name}' was deleted successfully.");
        }

        private string? ValidateLogo(IFormFile logo)
        {
            if (logo.Length > MaxFileSize)
            {
                return "Logo image must be smaller than 5 MB.";
            }

            var extension = Path.GetExtension(logo.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return "Only JPG, JPEG, PNG, WEBP and SVG images are allowed.";
            }

            return null;
        }

        private static async Task<string> SaveLogoAsync(IFormFile logo, string webRootPath)
        {
            var folder = Path.Combine(webRootPath, "images", "brands");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var extension = Path.GetExtension(logo.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(folder, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await logo.CopyToAsync(stream);

            return $"/images/brands/{fileName}";
        }

        private static void DeleteLogo(string? logoPath, string webRootPath)
        {
            if (string.IsNullOrWhiteSpace(logoPath))
                return;

            var fileName = Path.GetFileName(logoPath);
            var filePath = Path.Combine(webRootPath, "images", "brands", fileName);

            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
