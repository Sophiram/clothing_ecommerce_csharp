using System.Security.Claims;
using ClothingEcommerce.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;
using WebApplication_ClothingEcommerce.Services;
using WebApplication_ClothingEcommerce.Services.Interfaces;

namespace ClothingEcommerce.Server.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IProfileService profileService,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            ILogger<ProfileController> logger)
        {
            _profileService = profileService;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // GET: /api/profile?tab=overview
        [HttpGet]
        public async Task<IActionResult> GetProfile([FromQuery] string? tab = "overview")
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Fail("User is not authenticated.", 401));
            }

            var model = await _profileService.GetProfileViewModelAsync(userId, tab);
            if (model == null)
            {
                return NotFound(ApiResponse.Fail("Profile not found.", 404));
            }

            return Ok(ApiResponse<ProfileViewModel>.Ok(model));
        }

        // POST: /api/profile
        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileViewModel model)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _profileService.UpdateProfileAsync(userId, model);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.Fail(result.Errors.Any() ? string.Join(", ", result.Errors) : (result.Message ?? "Failed to update profile.")));
            }

            return Ok(ApiResponse.Ok(result.Message ?? "Profile updated successfully."));
        }

        // POST: /api/profile/address
        [HttpPost("address")]
        public async Task<IActionResult> AddAddress([FromBody] Address address)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _profileService.AddAddressAsync(userId, address);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.Fail(result.Message ?? "Failed to add address."));
            }

            return Ok(ApiResponse.Ok(result.Message ?? "Address added successfully."));
        }

        // PUT: /api/profile/address
        [HttpPut("address")]
        public async Task<IActionResult> EditAddress([FromBody] Address address)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _profileService.EditAddressAsync(userId, address);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.Fail(result.Message ?? "Failed to update address."));
            }

            return Ok(ApiResponse.Ok(result.Message ?? "Address updated successfully."));
        }

        // POST: /api/profile/address/{id}/default
        [HttpPost("address/{id:guid}/default")]
        public async Task<IActionResult> SetDefaultAddress(Guid id)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _profileService.SetDefaultAddressAsync(userId, id);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.Fail(result.Message ?? "Failed to set default address."));
            }

            return Ok(ApiResponse.Ok(result.Message ?? "Default address set successfully."));
        }

        // DELETE: /api/profile/address/{id}
        [HttpDelete("address/{id:guid}")]
        public async Task<IActionResult> DeleteAddress(Guid id)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _profileService.DeleteAddressAsync(userId, id);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.Fail(result.Message ?? "Failed to delete address."));
            }

            return Ok(ApiResponse.Ok(result.Message ?? "Address deleted successfully."));
        }

        // POST: /api/profile/change-password
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _profileService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.Fail(result.Errors.Any() ? string.Join(", ", result.Errors) : (result.Message ?? "Failed to change password.")));
            }

            return Ok(ApiResponse.Ok(result.Message ?? "Password changed successfully."));
        }

        // POST: /api/profile/avatar
        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile? avatar)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (avatar == null || avatar.Length == 0)
            {
                return BadRequest(ApiResponse.Fail("No avatar file provided."));
            }

            var result = await _profileService.UploadAvatarAsync(userId, avatar, _webHostEnvironment.WebRootPath);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.Fail(result.Errors.Any() ? string.Join(", ", result.Errors) : (result.Message ?? "Failed to upload avatar.")));
            }

            return Ok(ApiResponse.Ok(result.Message ?? "Avatar uploaded successfully."));
        }

        // DELETE: /api/profile/avatar
        [HttpDelete("avatar")]
        public async Task<IActionResult> RemoveAvatar()
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _profileService.RemoveAvatarAsync(userId, _webHostEnvironment.WebRootPath);
            return Ok(ApiResponse.Ok(result.Message ?? "Avatar removed successfully."));
        }
    }
}
