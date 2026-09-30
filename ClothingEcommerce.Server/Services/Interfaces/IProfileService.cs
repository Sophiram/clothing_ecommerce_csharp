using Microsoft.AspNetCore.Http;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Services
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new();

        public string? ErrorMessage => Errors.FirstOrDefault() ?? Message;
        public string? SuccessMessage => Message;

        public static ServiceResult Ok(string? message = null) => new() { Success = true, Message = message };
        public static ServiceResult Fail(string message) => new() { Success = false, Errors = new List<string> { message }, Message = message };
        public static ServiceResult Fail(IEnumerable<string> errors) => new() { Success = false, Errors = errors.ToList(), Message = errors.FirstOrDefault() };
    }

    public interface IProfileService
    {
        Task<ProfileViewModel?> GetProfileViewModelAsync(string userId, string? tab = "overview");
        Task<ServiceResult> UpdateProfileAsync(string userId, ProfileViewModel model);
        Task<ServiceResult> AddAddressAsync(string userId, Address address);
        Task<ServiceResult> EditAddressAsync(string userId, Address address);
        Task<ServiceResult> SetDefaultAddressAsync(string userId, Guid addressId);
        Task<ServiceResult> DeleteAddressAsync(string userId, Guid addressId);
        Task<ServiceResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
        Task<ServiceResult> UploadAvatarAsync(string userId, IFormFile avatar, string webRootPath);
        Task<ServiceResult> RemoveAvatarAsync(string userId, string webRootPath);
        Task<List<Address>> GetCustomerAddressesAsync(string userId);
    }
}
