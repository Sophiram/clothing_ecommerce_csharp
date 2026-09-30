using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public interface IDeliveryService
    {
        Task<List<DeliveryMethod>> GetActiveDeliveryMethodsAsync();
        Task<List<DeliveryMethod>> GetAllDeliveryMethodsAsync();
        Task<DeliveryMethod?> GetDeliveryMethodByCodeAsync(string code);
        Task<bool> UpdateDeliveryMethodAsync(DeliveryMethod method);

        Task<List<DeliveryBranch>> GetBranchesByCarrierAsync(string carrierCode = "VETExpress");
        Task<List<string>> GetProvincesAsync(string carrierCode = "VETExpress");
        Task<List<DeliveryBranch>> GetBranchesByProvinceAsync(string province, string carrierCode = "VETExpress");
        Task<bool> AddBranchAsync(DeliveryBranch branch);
        Task<bool> UpdateBranchAsync(DeliveryBranch branch);
        Task<bool> DeleteBranchAsync(Guid id);
    }
}
