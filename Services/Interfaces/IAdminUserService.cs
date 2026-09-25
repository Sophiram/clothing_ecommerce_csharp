using Microsoft.AspNetCore.Identity;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Services
{
    public interface IAdminUserService
    {
        // Admins
        Task<List<AdminListItemViewModel>> GetAdminsAsync(string? search, string? roleFilter, string currentUserId);
        Task<(int Total, int SuperAdmins, int Admins)> GetAdminCountsAsync();
        Task<AdminDetailsViewModel?> GetAdminDetailsAsync(string id, string currentUserId);
        Task<AdminEditViewModel?> GetAdminForEditAsync(string id);
        Task<ServiceResult> CreateAdminAsync(AdminCreateViewModel model, string? currentUserId, string? currentEmail, string? ip);
        Task<ServiceResult> UpdateAdminAsync(AdminEditViewModel model, string currentUserId, string? currentEmail, string? ip);
        Task<ServiceResult> ToggleAdminStatusAsync(string id, string currentUserId, string? currentEmail, string? ip);
        Task<ServiceResult> ResetAdminPasswordAsync(string id, string newPassword, string? currentUserId, string? currentEmail, string? ip);
        Task<ServiceResult> DeleteAdminAsync(string id, string currentUserId, string? currentEmail, string? ip);
        Task<ServiceResult> RevokeAdminRoleAsync(string id, string currentUserId, string? currentEmail, string? ip);
        Task<ServiceResult> AssignAdminRoleAsync(string email, string role, string? currentUserId, string? currentEmail, string? ip);

        // Users
        Task<(List<ApplicationUser> Users, Dictionary<string, IList<string>> UserRoles, Dictionary<string, bool> UserLockouts, Dictionary<string, string?> UserAvatars, List<string> AllRoles)> GetUsersAsync(string? search, string? roleFilter);
        Task<(ApplicationUser? User, IList<string> Roles)> GetUserDetailsAsync(string id);
        Task<bool> GetUserLockoutStatusAsync(string id);
        Task<List<string>> GetAllRoleNamesAsync();
        Task<ServiceResult> CreateUserAsync(string firstName, string lastName, string email, string password, string role, string? currentUserId, string? currentEmail, string? ip);
        Task<(ApplicationUser? User, IList<string> Roles)> GetUserForEditAsync(string id);
        Task<ServiceResult> UpdateUserAsync(string id, string firstName, string lastName, string email, string? phoneNumber, string role, string currentUserId, string? currentEmail, string? ip);
        Task<ServiceResult> DeleteUserAsync(string id, string currentUserId, string? currentEmail, string? ip);
        Task<ServiceResult> ToggleUserStatusAsync(string id, string currentUserId, string? currentEmail, string? ip);
        Task<ServiceResult> ResetUserPasswordAsync(string id, string newPassword, string confirmPassword, string? currentUserId, string? currentEmail, string? ip);

        // Roles
        Task<(List<IdentityRole> Roles, Dictionary<string, int> UserCounts, HashSet<string> SystemRoles)> GetRolesAsync();
        Task<ServiceResult> CreateRoleAsync(string roleName, string? currentUserId, string? currentEmail, string? ip);
        Task<ServiceResult> DeleteRoleAsync(string id, string? currentUserId, string? currentEmail, string? ip);
        Task<ServiceResult> EditRoleAsync(string id, string newName, string? currentUserId, string? currentEmail, string? ip);
        Task<(IdentityRole? Role, IList<ApplicationUser> Users, bool IsSystem)> GetUsersInRoleAsync(string roleName);

        // Settings
        Task SaveSettingsAsync(string storeName, string supportEmail, string supportPhone, string currency, bool maintenanceMode, bool allowRegistration, string? currentUserId, string? currentEmail, string? ip);
    }
}
