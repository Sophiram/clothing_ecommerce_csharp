using Microsoft.AspNetCore.Http;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services.Interfaces
{
    public interface IAvatarService
    {
        string? GetAvatarUrl(string userId);
        Task<ServiceResult> UploadAvatarAsync(string userId, IFormFile avatarFile);
        Task<ServiceResult> DeleteAvatarAsync(string userId);
    }
}
