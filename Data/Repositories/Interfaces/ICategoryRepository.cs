using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Interfaces
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<IReadOnlyList<Category>> GetAllWithProductsAsync();
        Task<Category?> GetCategoryWithProductsAsync(Guid id);
        Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);
    }
}
