using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<AuthResult> RegisterAsync(RegisterViewModel model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.Phone,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync("User"))
                    await _roleManager.CreateAsync(new IdentityRole("User"));

                await _userManager.AddToRoleAsync(user, "User");

                var customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    ApplicationUserId = user.Id,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Phone = model.Phone,
                    Status = CustomerStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(user, isPersistent: false);

                return new AuthResult { Succeeded = true };
            }

            return new AuthResult
            {
                Succeeded = false,
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }

        public async Task<AuthResult> LoginAsync(LoginViewModel model)
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null && await _userManager.IsLockedOutAsync(existingUser))
            {
                return new AuthResult
                {
                    Succeeded = false,
                    IsLockedOut = true,
                    Errors = new List<string> { "This account has been deactivated. Please contact an administrator." }
                };
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                return new AuthResult
                {
                    Succeeded = false,
                    IsLockedOut = true,
                    Errors = new List<string> { "This account has been temporarily locked due to multiple failed login attempts. Please try again later." }
                };
            }

            if (result.Succeeded)
            {
                var isStaff = existingUser != null && (
                    await _userManager.IsInRoleAsync(existingUser, "SuperAdmin") ||
                    await _userManager.IsInRoleAsync(existingUser, "Admin"));

                return new AuthResult
                {
                    Succeeded = true,
                    IsStaff = isStaff
                };
            }

            return new AuthResult
            {
                Succeeded = false,
                Errors = new List<string> { "Invalid email or password." }
            };
        }

        public async Task<AuthResult> ExternalLoginSignInAsync(ExternalLoginInfo info)
        {
            if (info == null)
            {
                return new AuthResult
                {
                    Succeeded = false,
                    Errors = new List<string> { "External login information was not provided." }
                };
            }

            // 1. Attempt to sign in with an already-linked external login
            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

            if (signInResult.IsLockedOut)
            {
                return new AuthResult
                {
                    Succeeded = false,
                    IsLockedOut = true,
                    Errors = new List<string> { "This account has been deactivated. Please contact an administrator." }
                };
            }

            if (signInResult.Succeeded)
            {
                var existingUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                var isStaff = existingUser != null && (
                    await _userManager.IsInRoleAsync(existingUser, "SuperAdmin") ||
                    await _userManager.IsInRoleAsync(existingUser, "Admin"));

                return new AuthResult
                {
                    Succeeded = true,
                    IsStaff = isStaff
                };
            }

            // 2. Not linked yet -> retrieve user email from external claims
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                return new AuthResult
                {
                    Succeeded = false,
                    Errors = new List<string> { "Email claim could not be retrieved from Google." }
                };
            }

            // Extract Name components
            var givenName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
            var surname = info.Principal.FindFirstValue(ClaimTypes.Surname);
            var fullName = info.Principal.FindFirstValue(ClaimTypes.Name);

            string firstName = givenName ?? "";
            string lastName = surname ?? "";

            if (string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(fullName))
            {
                var nameParts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                firstName = nameParts[0];
                lastName = nameParts.Length > 1 ? nameParts[1] : "";
            }

            if (string.IsNullOrWhiteSpace(firstName))
            {
                firstName = email.Split('@')[0];
            }

            // 3. Check if user already exists by Email
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                if (await _userManager.IsLockedOutAsync(user))
                {
                    return new AuthResult
                    {
                        Succeeded = false,
                        IsLockedOut = true,
                        Errors = new List<string> { "This account has been deactivated. Please contact an administrator." }
                    };
                }

                // Link the external login
                var linkResult = await _userManager.AddLoginAsync(user, info);
                if (!linkResult.Succeeded)
                {
                    return new AuthResult
                    {
                        Succeeded = false,
                        Errors = linkResult.Errors.Select(e => e.Description).ToList()
                    };
                }

                // Ensure customer record exists
                var hasCustomer = await _context.Customers.AnyAsync(c => c.ApplicationUserId == user.Id);
                if (!hasCustomer)
                {
                    var customer = new Customer
                    {
                        Id = Guid.NewGuid(),
                        ApplicationUserId = user.Id,
                        FirstName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : firstName,
                        LastName = !string.IsNullOrWhiteSpace(user.LastName) ? user.LastName : lastName,
                        Email = user.Email ?? email,
                        Phone = user.PhoneNumber,
                        Status = CustomerStatus.Active,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                await _signInManager.SignInAsync(user, isPersistent: false);

                var isStaff = await _userManager.IsInRoleAsync(user, "SuperAdmin") ||
                              await _userManager.IsInRoleAsync(user, "Admin");

                return new AuthResult
                {
                    Succeeded = true,
                    IsStaff = isStaff
                };
            }

            // 4. Create a brand new user
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return new AuthResult
                {
                    Succeeded = false,
                    Errors = createResult.Errors.Select(e => e.Description).ToList()
                };
            }

            // Add Role "User"
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }
            await _userManager.AddToRoleAsync(user, "User");

            // Link external login to newly created user
            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
            {
                return new AuthResult
                {
                    Succeeded = false,
                    Errors = addLoginResult.Errors.Select(e => e.Description).ToList()
                };
            }

            // Create linked Customer profile
            var newCustomer = new Customer
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = user.Id,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = "",
                Status = CustomerStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            _context.Customers.Add(newCustomer);
            await _context.SaveChangesAsync();

            // Sign in newly created user
            await _signInManager.SignInAsync(user, isPersistent: false);

            return new AuthResult
            {
                Succeeded = true,
                IsStaff = false
            };
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}
