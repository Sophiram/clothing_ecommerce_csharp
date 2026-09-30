using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Services
{
    public interface IDashboardService
    {
        Task<SuperAdminDashboardViewModel> GetSuperAdminDashboardAsync();
        Task<StoreAdminDashboardViewModel> GetStoreAdminDashboardAsync();
        Task<AdminDashboardViewModel> GetDashboardAsync(bool isSuperAdmin, string? requestedView = null);
    }
}
