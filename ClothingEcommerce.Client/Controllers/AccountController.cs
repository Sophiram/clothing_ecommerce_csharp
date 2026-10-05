using System.Security.Claims;
using ClothingEcommerce.Client.Services.ApiClient;
using ClothingEcommerce.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace ClothingEcommerce.Client.Controllers
{
    public class AccountController : Controller
    {
        private readonly IApiClient _apiClient;

        public AccountController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return LocalRedirect(returnUrl ?? "/");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var request = new LoginRequestDto
            {
                Email = model.Email,
                Password = model.Password,
                RememberMe = model.RememberMe
            };

            var response = await _apiClient.PostAsync<LoginRequestDto, AuthResponseDto>("api/auth/login", request);
            if (response == null || !response.Success || response.Data?.Token == null)
            {
                ModelState.AddModelError(string.Empty, response?.Message ?? "Invalid email or password.");
                return View(model);
            }

            await SignInWithJwtAsync(response.Data);

            var redirectTarget = returnUrl ?? model.ReturnUrl;
            if (!string.IsNullOrEmpty(redirectTarget) && Url.IsLocalUrl(redirectTarget))
            {
                return Redirect(redirectTarget);
            }

            if (response.Data.Roles.Contains("Cashier"))
            {
                return RedirectToAction("Index", "Pos", new { area = "Admin" });
            }

            if (response.Data.Roles.Contains("Staff"))
            {
                return RedirectToAction("Index", "Fulfillment", new { area = "Admin" });
            }

            if (response.Data.Roles.Contains("SuperAdmin") || response.Data.Roles.Contains("Admin") || response.Data.Roles.Contains("Manager"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var request = new RegisterRequestDto
            {
                Email = model.Email,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.Phone
            };

            var response = await _apiClient.PostAsync<RegisterRequestDto, AuthResponseDto>("api/auth/register", request);
            if (response == null || !response.Success || response.Data?.Token == null)
            {
                if (response?.Errors.Any() == true)
                {
                    foreach (var err in response.Errors)
                    {
                        ModelState.AddModelError(string.Empty, err);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, response?.Message ?? "Registration failed.");
                }
                return View(model);
            }

            await SignInWithJwtAsync(response.Data);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Delete("jwt_token");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private async Task SignInWithJwtAsync(AuthResponseDto auth)
        {
            // Store token in HttpOnly cookie for outgoing API calls
            Response.Cookies.Append("jwt_token", auth.Token!, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = auth.Expiration ?? DateTimeOffset.UtcNow.AddDays(7)
            });

            // Extract claims from token or DTO
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, auth.UserId ?? string.Empty),
                new(ClaimTypes.Email, auth.Email ?? string.Empty),
                new(ClaimTypes.Name, auth.FullName ?? auth.Email ?? string.Empty)
            };

            foreach (var role in auth.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = auth.Expiration ?? DateTimeOffset.UtcNow.AddDays(7)
            });
        }
    }
}
