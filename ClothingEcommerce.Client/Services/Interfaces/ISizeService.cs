using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public interface ISizeService
    {
        Task<List<Size>> GetAllAsync();
        Task<Size?> GetByIdAsync(Guid id);
        Task<ServiceResult> CreateAsync(Size size);
        Task<ServiceResult> UpdateAsync(Guid id, Size size);
        Task<ServiceResult> DeleteAsync(Guid id);
    }
}
