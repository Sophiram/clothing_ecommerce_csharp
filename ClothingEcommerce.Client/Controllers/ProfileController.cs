using System.Security.Claims;
using ClothingEcommerce.Client.Services.ApiClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace ClothingEcommerce.Client.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly WebApplication_ClothingEcommerce.Services.IProfileService _profileService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IApiClient apiClient, 
            WebApplication_ClothingEcommerce.Services.IProfileService profileService,
            ILogger<ProfileController> logger)
        {
            _apiClient = apiClient;
            _profileService = profileService;
            _logger = logger;
        }

        // =====================================================
        // PROFILE DASHBOARD
        // GET: /Profile
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? tab = "overview")
        {
            var response = await _apiClient.GetAsync<ProfileViewModel>($"api/profile?tab={Uri.EscapeDataString(tab ?? "overview")}");
            if (response != null && response.Success && response.Data != null)
            {
                response.Data.ActiveTab = tab ?? "overview";
                return View("Index", response.Data);
            }

            // Robust fallback using direct ProfileService & database
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var localModel = await _profileService.GetProfileViewModelAsync(userId, tab);
                if (localModel != null)
                {
                    localModel.ActiveTab = tab ?? "overview";
                    return View("Index", localModel);
                }
            }

            // Fallback default model if profile is newly created
            var fallbackModel = new ProfileViewModel
            {
                FirstName = User.FindFirstValue(ClaimTypes.Name)?.Split(' ').FirstOrDefault() ?? "Customer",
                LastName = User.FindFirstValue(ClaimTypes.Name)?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                Email = User.FindFirstValue(ClaimTypes.Email) ?? (User.Identity?.Name?.Contains('@') == true ? User.Identity.Name : ""),
                ActiveTab = tab ?? "overview"
            };
            return View("Index", fallbackModel);
        }

        // =====================================================
        // UPDATE PROFILE
        // POST: /Profile/Index
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["Error"] = string.IsNullOrWhiteSpace(errors) ? "Please check your personal information." : errors;
                return RedirectToAction(nameof(Index), new { tab = "profile" });
            }

            var response = await _apiClient.PostAsync("api/profile", model);
            if (response != null && response.Success)
            {
                TempData["Success"] = response.Message ?? "Profile updated successfully.";
                return RedirectToAction(nameof(Index), new { tab = "profile" });
            }

            // Fallback to local profile service directly if API failed or unauthenticated
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var localResult = await _profileService.UpdateProfileAsync(userId, model);
                if (localResult.Success)
                {
                    TempData["Success"] = localResult.Message ?? "Profile updated successfully.";
                    return RedirectToAction(nameof(Index), new { tab = "profile" });
                }
                TempData["Error"] = localResult.Message ?? (localResult.Errors.Any() ? string.Join(", ", localResult.Errors) : "Failed to update profile.");
                return RedirectToAction(nameof(Index), new { tab = "profile" });
            }

            TempData["Error"] = response?.Message ?? "Failed to update profile.";
            return RedirectToAction(nameof(Index), new { tab = "profile" });
        }

        // =====================================================
        // ADD ADDRESS
        // POST: /Profile/AddAddress
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(Address address)
        {
            var response = await _apiClient.PostAsync("api/profile/address", address);
            if (response != null && response.Success)
            {
                TempData["Success"] = response.Message ?? "Address added successfully.";
            }
            else
            {
                TempData["Error"] = response?.Message ?? "Failed to add address.";
            }

            return RedirectToAction(nameof(Index), new { tab = "addresses" });
        }

        // =====================================================
        // EDIT ADDRESS
        // POST: /Profile/EditAddress
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(Address model)
        {
            var response = await _apiClient.PutAsync("api/profile/address", model);
            if (response != null && response.Success)
            {
                TempData["Success"] = response.Message ?? "Address updated successfully.";
            }
            else
            {
                TempData["Error"] = response?.Message ?? "Failed to update address.";
            }

            return RedirectToAction(nameof(Index), new { tab = "addresses" });
        }

        // =====================================================
        // SET DEFAULT ADDRESS
        // POST: /Profile/SetDefaultAddress
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(Guid id)
        {
            var response = await _apiClient.PostAsync($"api/profile/address/{id}/default", new { });
            if (response != null && response.Success)
            {
                TempData["Success"] = response.Message ?? "Default address updated.";
            }
            else
            {
                TempData["Error"] = response?.Message ?? "Failed to set default address.";
            }

            return RedirectToAction(nameof(Index), new { tab = "addresses" });
        }

        // =====================================================
        // DELETE ADDRESS
        // POST: /Profile/DeleteAddress
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(Guid id)
        {
            var response = await _apiClient.DeleteAsync($"api/profile/address/{id}");
            if (response != null && response.Success)
            {
                TempData["Success"] = response.Message ?? "Address deleted successfully.";
            }
            else
            {
                TempData["Error"] = response?.Message ?? "Failed to delete address.";
            }

            return RedirectToAction(nameof(Index), new { tab = "addresses" });
        }

        // =====================================================
        // CHANGE PASSWORD
        // POST: /Profile/ChangePassword
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index), new { tab = "security" });
            }

            var response = await _apiClient.PostAsync("api/profile/change-password", model);
            if (response != null && response.Success)
            {
                TempData["Success"] = response.Message ?? "Password changed successfully.";
            }
            else
            {
                TempData["Error"] = response?.Message ?? "Failed to change password.";
            }

            return RedirectToAction(nameof(Index), new { tab = "security" });
        }

        // =====================================================
        // UPLOAD AVATAR
        // POST: /Profile/UploadAvatar
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile? avatar)
        {
            if (avatar == null || avatar.Length == 0)
            {
                TempData["Error"] = "Please select an image file to upload.";
                return RedirectToAction(nameof(Index), new { tab = "overview" });
            }

            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(avatar.OpenReadStream());
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(avatar.ContentType);
            content.Add(streamContent, "avatar", avatar.FileName);

            var response = await _apiClient.PostMultipartAsync("api/profile/avatar", content);
            if (response != null && response.Success)
            {
                TempData["Success"] = response.Message ?? "Avatar uploaded successfully.";
            }
            else
            {
                TempData["Error"] = response?.Message ?? "Failed to upload avatar.";
            }

            return RedirectToAction(nameof(Index), new { tab = "overview" });
        }

        // =====================================================
        // REMOVE AVATAR
        // POST: /Profile/RemoveAvatar
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAvatar()
        {
            var response = await _apiClient.DeleteAsync("api/profile/avatar");
            if (response != null && response.Success)
            {
                TempData["Success"] = response.Message ?? "Avatar removed successfully.";
            }
            else
            {
                TempData["Error"] = response?.Message ?? "Failed to remove avatar.";
            }

            return RedirectToAction(nameof(Index), new { tab = "overview" });
        }
    }
}
