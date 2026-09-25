using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IProfileService _profileService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(
            IProfileService profileService,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _profileService = profileService;
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // =====================================================
        // PROFILE DASHBOARD
        // GET: /Profile
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? tab = "overview")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var model = await _profileService.GetProfileViewModelAsync(user.Id, tab);
            if (model == null)
            {
                TempData["Error"] = "Customer profile could not be loaded.";
                return RedirectToAction("Index", "Home");
            }

            return View("Index", model);
        }

        // =====================================================
        // UPDATE PROFILE
        // POST: /Profile/Index
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!ModelState.IsValid)
            {
                model.Addresses = await _profileService.GetCustomerAddressesAsync(user.Id);
                model.ActiveTab = "profile";
                return View("Index", model);
            }

            var result = await _profileService.UpdateProfileAsync(user.Id, model);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = string.Join(" ", result.Errors);
            }

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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var result = await _profileService.AddAddressAsync(user.Id, address);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var result = await _profileService.EditAddressAsync(user.Id, model);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var result = await _profileService.SetDefaultAddressAsync(user.Id, id);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var result = await _profileService.DeleteAddressAsync(user.Id, id);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
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

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var result = await _profileService.ChangePasswordAsync(user.Id, model.CurrentPassword, model.NewPassword);
            if (result.Success)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = string.Join(" ", result.Errors);
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (avatar == null)
            {
                TempData["Error"] = "Please select an image to upload.";
                return RedirectToAction(nameof(Index), new { tab = "overview" });
            }

            var result = await _profileService.UploadAvatarAsync(user.Id, avatar, _webHostEnvironment.WebRootPath);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = string.Join(" ", result.Errors);
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var result = await _profileService.RemoveAvatarAsync(user.Id, _webHostEnvironment.WebRootPath);
            TempData["Success"] = result.Message;

            return RedirectToAction(nameof(Index), new { tab = "overview" });
        }
    }
}
