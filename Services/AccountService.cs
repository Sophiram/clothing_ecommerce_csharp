using Microsoft.AspNetCore.Identity;
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
                    Errors = new List<string> { "This account has been deactivated. Please contact an administrator." }
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

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}
