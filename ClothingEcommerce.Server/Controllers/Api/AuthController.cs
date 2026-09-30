using System.Security.Claims;
using ClothingEcommerce.Server.Services.Auth;
using ClothingEcommerce.Shared.Common;
using ClothingEcommerce.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;

namespace ClothingEcommerce.Server.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<AuthResponseDto>.Fail(errors));
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Invalid email or password.", 401));
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Account is temporarily locked out. Please try again later.", 401));
                }
                return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Invalid email or password.", 401));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenService.GenerateJwtToken(user, roles);

            var response = new AuthResponseDto
            {
                Success = true,
                Token = token,
                Expiration = DateTime.UtcNow.AddDays(7),
                UserId = user.Id,
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Roles = roles.ToList(),
                Message = "Login successful."
            };

            return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Login successful."));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<AuthResponseDto>.Fail(errors));
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(ApiResponse<AuthResponseDto>.Fail("An account with this email address already exists."));
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(ApiResponse<AuthResponseDto>.Fail(errors));
            }

            // Assign default User role
            await _userManager.AddToRoleAsync(user, "User");
            var roles = new List<string> { "User" };

            var token = _tokenService.GenerateJwtToken(user, roles);

            var response = new AuthResponseDto
            {
                Success = true,
                Token = token,
                Expiration = DateTime.UtcNow.AddDays(7),
                UserId = user.Id,
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Roles = roles,
                Message = "Registration successful."
            };

            return Ok(ApiResponse<AuthResponseDto>.Created(response, "Registration successful."));
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<UserProfileDto>.Fail("Unauthorized", 401));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<UserProfileDto>.Fail("User not found.", 404));
            }

            var roles = await _userManager.GetRolesAsync(user);

            var profile = new UserProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Roles = roles.ToList()
            };

            return Ok(ApiResponse<UserProfileDto>.Ok(profile));
        }
    }
}
