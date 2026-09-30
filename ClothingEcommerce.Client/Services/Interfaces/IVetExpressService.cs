using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public interface IVetExpressService
    {
        Task<List<string>> GetProvincesAsync();
        Task<List<DeliveryBranch>> GetBranchesByProvinceAsync(string province);
        Task<decimal> CalculateDeliveryFeeAsync(string province, decimal totalAmount);
        Task<string> CreateWaybillAsync(Order order, string branchName);
    }
}
