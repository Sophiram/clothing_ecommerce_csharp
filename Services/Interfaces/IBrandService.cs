using Microsoft.AspNetCore.Http;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class BrandResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new();
        public Brand? Brand { get; set; }
    }

    public interface IBrandService
    {
        Task<List<Brand>> GetBrandsAsync(string? search = null);
        Task<Brand?> GetBrandDetailsAsync(Guid id);
        Task<BrandResult> CreateBrandAsync(Brand brand, IFormFile? logo, string? logoUrl, string webRootPath);
        Task<BrandResult> UpdateBrandAsync(Guid id, Brand brand, IFormFile? logo, string? logoUrl, bool removeLogo, string webRootPath);
        Task<ServiceResult> DeleteBrandAsync(Guid id, string webRootPath);
    }
}
