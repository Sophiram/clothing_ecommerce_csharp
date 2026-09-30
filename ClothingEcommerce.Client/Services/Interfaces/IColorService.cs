using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public interface IColorService
    {
        Task<List<Color>> GetAllAsync();
        Task<Color?> GetByIdAsync(Guid id);
        Task<ServiceResult> CreateAsync(Color color);
        Task<ServiceResult> UpdateAsync(Guid id, Color color);
        Task<ServiceResult> DeleteAsync(Guid id);
    }
}
