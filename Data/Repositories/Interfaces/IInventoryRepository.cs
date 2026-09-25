using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Interfaces
{
    public interface IInventoryRepository : IRepository<Inventory>
    {
        Task<Inventory?> GetByVariantIdAsync(Guid variantId);
        Task<IReadOnlyList<Inventory>> GetAllWithDetailsAsync();
        Task<IReadOnlyList<Inventory>> GetLowStockInventoriesAsync(int threshold);
    }
}
