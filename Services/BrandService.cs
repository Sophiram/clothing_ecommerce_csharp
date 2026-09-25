using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data.Repositories.Interfaces;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class BrandService : IBrandService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public BrandService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<Brand>> GetBrandsAsync(string? search = null)
        {
            var brands = await _unitOfWork.Brands.GetAllWithProductsAsync();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                return brands.Where(b => b.Name.ToLower().Contains(s) || (b.Description != null && b.Description.ToLower().Contains(s))).ToList();
            }
            return brands.ToList();
        }

        public async Task<Brand?> GetBrandDetailsAsync(Guid id)
        {
            return await _unitOfWork.Brands.GetBrandWithProductsAsync(id);
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
            await _unitOfWork.Brands.AddAsync(brand);
            await _unitOfWork.SaveChangesAsync();

            return new BrandResult
            {
                Success = true,
                Message = $"Brand '{brand.Name}' was created successfully.",
                Brand = brand
            };
        }

        public async Task<BrandResult> UpdateBrandAsync(Guid id, Brand brand, IFormFile? logo, string? logoUrl, bool removeLogo, string webRootPath)
        {
            var existingBrand = await _unitOfWork.Brands.GetByIdAsync(id);
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
                if (!string.IsNullOrWhiteSpace(oldLogo) && oldLogo.StartsWith("/images/"))
                    DeleteLogo(oldLogo, webRootPath);
            }
            else if (!string.IsNullOrWhiteSpace(logoUrl))
            {
                if (Uri.TryCreate(logoUrl.Trim(), UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
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

            _unitOfWork.Brands.Update(existingBrand);
            await _unitOfWork.SaveChangesAsync();

            return new BrandResult
            {
                Success = true,
                Message = $"Brand '{existingBrand.Name}' was updated successfully.",
                Brand = existingBrand
            };
        }

        public async Task<ServiceResult> DeleteBrandAsync(Guid id, string webRootPath)
        {
            var brand = await _unitOfWork.Brands.GetBrandWithProductsAsync(id);
            if (brand == null)
            {
                return ServiceResult.Fail("Brand not found.");
            }

            if (brand.Products.Any())
            {
                return ServiceResult.Fail("This brand cannot be deleted because it has products.");
            }

            var name = brand.Name;
            DeleteLogo(brand.Logo, webRootPath);
            _unitOfWork.Brands.Remove(brand);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Ok($"Brand '{name}' was deleted successfully.");
        }

        private string? ValidateLogo(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(ext))
            {
                return $"Invalid file type '{ext}'. Allowed: {string.Join(", ", _allowedExtensions)}";
            }
            if (file.Length > MaxFileSize)
            {
                return $"File size ({file.Length / 1024 / 1024} MB) exceeds maximum 5 MB.";
            }
            return null;
        }

        private async Task<string> SaveLogoAsync(IFormFile file, string webRootPath)
        {
            var uploadsFolder = Path.Combine(webRootPath, "images", "brands");
            Directory.CreateDirectory(uploadsFolder);
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueFileName = $"brand_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/images/brands/{uniqueFileName}";
        }

        private void DeleteLogo(string? relativePath, string webRootPath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            var fullPath = Path.Combine(webRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
            {
                try { File.Delete(fullPath); } catch { }
            }
        }
    }
}
