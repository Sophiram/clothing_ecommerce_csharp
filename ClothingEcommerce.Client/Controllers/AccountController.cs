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
        private readonly IConfiguration _configuration;

        public AccountController(IApiClient apiClient, IConfiguration configuration)
        {
            _apiClient = apiClient;
            _configuration = configuration;
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

        // ==========================================
        // EXTERNAL LOGIN (GOOGLE)
        // ==========================================
        [HttpPost]
        [HttpGet]
        public IActionResult ExternalLogin(string provider = "Google", string? returnUrl = null)
        {
            var googleClientId = _configuration["GOOGLE_CLIENT_ID"];
            var googleClientSecret = _configuration["GOOGLE_CLIENT_SECRET"];

            if (string.IsNullOrWhiteSpace(googleClientId) ||
                string.IsNullOrWhiteSpace(googleClientSecret) ||
                googleClientId.Contains("your-google", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Google OAuth is not configured yet. Please supply a valid GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET in .env.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (!string.IsNullOrEmpty(remoteError))
            {
                TempData["ErrorMessage"] = $"External login error: {remoteError}";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var authResult = await HttpContext.AuthenticateAsync("ExternalCookie");
            if (!authResult.Succeeded || authResult.Principal == null)
            {
                TempData["ErrorMessage"] = "Failed to authenticate with Google.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Email claim could not be retrieved from Google.";
                await HttpContext.SignOutAsync("ExternalCookie");
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var providerKey = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? email;
            var fullName = authResult.Principal.FindFirstValue(ClaimTypes.Name);
            var givenName = authResult.Principal.FindFirstValue(ClaimTypes.GivenName);
            var surname = authResult.Principal.FindFirstValue(ClaimTypes.Surname);

            var request = new ExternalLoginRequestDto
            {
                Provider = "Google",
                ProviderKey = providerKey,
                Email = email,
                FirstName = givenName ?? (fullName != null ? fullName.Split(' ').FirstOrDefault() : ""),
                LastName = surname ?? (fullName != null && fullName.Contains(' ') ? fullName[(fullName.IndexOf(' ') + 1)..] : ""),
                FullName = fullName ?? email
            };

            var response = await _apiClient.PostAsync<ExternalLoginRequestDto, AuthResponseDto>("api/auth/external-login", request);

            // Clear temporary external cookie
            await HttpContext.SignOutAsync("ExternalCookie");

            if (response == null || !response.Success || response.Data?.Token == null)
            {
                TempData["ErrorMessage"] = response?.Message ?? "Unable to complete external sign in.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            await SignInWithJwtAsync(response.Data);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
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
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Delete("jwt_token");
            HttpContext.Session.Remove("jwt_token");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private async Task SignInWithJwtAsync(AuthResponseDto auth)
        {
            // Store token in HttpOnly cookie and session for outgoing API calls
            if (!string.IsNullOrEmpty(auth.Token))
            {
                Response.Cookies.Append("jwt_token", auth.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = auth.Expiration ?? DateTimeOffset.UtcNow.AddDays(7)
                });
                HttpContext.Session.SetString("jwt_token", auth.Token);
            }

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
