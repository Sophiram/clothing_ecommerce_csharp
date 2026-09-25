using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetCategoriesAsync(string? search = null);
        Task<Category?> GetByIdAsync(Guid id);
        Task<ServiceResult> CreateAsync(Category category);
        Task<ServiceResult> UpdateAsync(Guid id, Category category);
        Task<ServiceResult> DeleteAsync(Guid id);
    }
}
