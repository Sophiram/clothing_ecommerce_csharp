using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Interfaces
{
    public interface IBrandRepository : IRepository<Brand>
    {
        Task<IReadOnlyList<Brand>> GetAllWithProductsAsync();
        Task<Brand?> GetBrandWithProductsAsync(Guid id);
        Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);
    }
}
