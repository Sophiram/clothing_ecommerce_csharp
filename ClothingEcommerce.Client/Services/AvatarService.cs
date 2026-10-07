using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services.Interfaces;

namespace WebApplication_ClothingEcommerce.Services
{
    public class AvatarService : IAvatarService
    {
        private readonly IWebHostEnvironment _environment;

        public AvatarService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public string? GetAvatarUrl(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var folder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
            if (!Directory.Exists(folder)) return null;

            var files = Directory.GetFiles(folder, $"{userId}.*");
            if (files.Length > 0)
            {
                var fileInfo = new FileInfo(files[0]);
                return $"/uploads/avatars/{Path.GetFileName(files[0])}?v={fileInfo.LastWriteTimeUtc.Ticks}";
            }

            return null;
        }

        public async Task<ServiceResult> UploadAvatarAsync(string userId, IFormFile avatarFile)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return ServiceResult.Fail("Invalid user identifier.");
            }

            if (avatarFile == null || avatarFile.Length == 0)
            {
                return ServiceResult.Fail("Please select a profile image file to upload.");
            }

            if (avatarFile.Length > 5 * 1024 * 1024)
            {
                return ServiceResult.Fail("Avatar image size must not exceed 5MB.");
            }

            var ext = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(ext))
            {
                return ServiceResult.Fail("Only JPG, PNG, and WebP image formats are supported.");
            }

            var folder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // Remove previous avatar files for this user
            var existingFiles = Directory.GetFiles(folder, $"{userId}.*");
            foreach (var f in existingFiles)
            {
                try { File.Delete(f); } catch { }
            }

            var savePath = Path.Combine(folder, $"{userId}{ext}");
            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await avatarFile.CopyToAsync(stream);
            }

            return ServiceResult.Ok("Profile photo updated successfully!");
        }

        public Task<ServiceResult> DeleteAvatarAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Task.FromResult(ServiceResult.Fail("Invalid user identifier."));
            }

            var folder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
            if (Directory.Exists(folder))
            {
                var files = Directory.GetFiles(folder, $"{userId}.*");
                foreach (var f in files)
                {
                    try { File.Delete(f); } catch { }
                }
            }

            return Task.FromResult(ServiceResult.Ok("Profile photo removed."));
        }
    }
}
