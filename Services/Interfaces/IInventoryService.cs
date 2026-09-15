using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class VariantDropdownItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IInventoryService
    {
        Task<List<Inventory>> GetInventoriesAsync(Guid? productId);
        Task<Inventory?> GetInventoryDetailsAsync(Guid id);
        Task<List<Product>> GetAllProductsAsync();
        Task<List<VariantDropdownItem>> GetVariantDropdownItemsAsync();
        Task<ServiceResult> CreateInventoryAsync(Inventory inventory);
        Task<ServiceResult> UpdateInventoryAsync(Guid id, Inventory inventory);
        Task<ServiceResult> DeleteInventoryAsync(Guid id);
    }
}
