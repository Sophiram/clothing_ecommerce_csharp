using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

using WebApplication_ClothingEcommerce.Services.Interfaces;

namespace WebApplication_ClothingEcommerce.Services
{
    public class ProfileService : IProfileService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAvatarService _avatarService;

        public ProfileService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IAvatarService avatarService)
        {
            _context = context;
            _userManager = userManager;
            _avatarService = avatarService;
        }

        private async Task<Customer?> GetOrCreateCustomerAsync(ApplicationUser user)
        {
            var customer = await _context.Customers
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id || c.Email == user.Email);

            if (customer == null && !string.IsNullOrWhiteSpace(user.Email))
            {
                customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    ApplicationUserId = user.Id,
                    FirstName = user.FirstName ?? user.Email.Split('@')[0],
                    LastName = user.LastName ?? "",
                    Email = user.Email,
                    Phone = user.PhoneNumber ?? "",
                    Status = CustomerStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }
            else if (customer != null && string.IsNullOrEmpty(customer.ApplicationUserId))
            {
                customer.ApplicationUserId = user.Id;
                await _context.SaveChangesAsync();
            }

            return customer;
        }

        public async Task<ProfileViewModel?> GetProfileViewModelAsync(string userId, string? tab = "overview")
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var customer = await GetOrCreateCustomerAsync(user);
            if (customer == null) return null;

            var addresses = await _context.Addresses
                .AsNoTracking()
                .Where(a => a.CustomerId == customer.Id)
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.City)
                .ToListAsync();

            var ordersQuery = _context.Orders.Where(o => o.CustomerId == customer.Id);
            var totalOrders = await ordersQuery.CountAsync();
            var pendingOrders = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Pending);
            var completedOrders = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Delivered);
            var totalSpent = await ordersQuery
                .Where(o => o.Status != OrderStatus.Cancelled)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var wishlistCount = await _context.Wishlists
                .Where(w => w.CustomerId == customer.Id)
                .SelectMany(w => w.Items)
                .CountAsync();

            var cartCount = await _context.Carts
                .Where(c => c.CustomerId == customer.Id)
                .SelectMany(c => c.Items)
                .SumAsync(i => (int?)i.Quantity) ?? 0;

            var recentOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CustomerId == customer.Id)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)
                .ToListAsync();

            return new ProfileViewModel
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Phone = customer.Phone ?? string.Empty,
                Addresses = addresses,
                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                CompletedOrders = completedOrders,
                TotalSpent = totalSpent,
                WishlistCount = wishlistCount,
                CartCount = cartCount,
                MemberSince = customer.CreatedAt != default ? customer.CreatedAt : DateTime.UtcNow,
                Status = customer.Status.ToString(),
                RecentOrders = recentOrders,
                ProfileImageUrl = _avatarService.GetAvatarUrl(user.Id),
                ActiveTab = string.IsNullOrWhiteSpace(tab) ? "overview" : tab.ToLowerInvariant()
            };
        }

        public async Task<ServiceResult> UpdateProfileAsync(string userId, ProfileViewModel model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.Fail("User not found.");

            var customer = await GetOrCreateCustomerAsync(user);
            if (customer == null) return ServiceResult.Fail("Customer profile not found.");

            customer.FirstName = model.FirstName.Trim();
            customer.LastName = model.LastName.Trim();
            customer.Phone = model.Phone?.Trim();

            user.FirstName = customer.FirstName;
            user.LastName = customer.LastName;
            user.PhoneNumber = customer.Phone;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return ServiceResult.Fail(updateResult.Errors.Select(e => e.Description));
            }

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Your profile details have been successfully updated.");
        }

        public async Task<ServiceResult> AddAddressAsync(string userId, Address address)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.Fail("User not found.");

            var customer = await GetOrCreateCustomerAsync(user);
            if (customer == null) return ServiceResult.Fail("Customer not found.");

            var hasAddress = await _context.Addresses.AnyAsync(a => a.CustomerId == customer.Id);

            address.Id = Guid.NewGuid();
            address.CustomerId = customer.Id;

            if (!hasAddress || address.IsDefault)
            {
                if (address.IsDefault)
                {
                    var existingDefaults = await _context.Addresses
                        .Where(a => a.CustomerId == customer.Id && a.IsDefault)
                        .ToListAsync();
                    foreach (var d in existingDefaults)
                    {
                        d.IsDefault = false;
                    }
                }
                address.IsDefault = true;
            }

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("New address added successfully.");
        }

        public async Task<ServiceResult> EditAddressAsync(string userId, Address model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.Fail("User not found.");

            var customer = await GetOrCreateCustomerAsync(user);
            if (customer == null) return ServiceResult.Fail("Customer not found.");

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == model.Id && a.CustomerId == customer.Id);

            if (address == null) return ServiceResult.Fail("Address not found.");

            address.Street = model.Street ?? string.Empty;
            address.City = model.City ?? string.Empty;
            address.Province = model.Province ?? string.Empty;
            address.PostalCode = model.PostalCode ?? string.Empty;

            if (model.IsDefault && !address.IsDefault)
            {
                var otherDefaults = await _context.Addresses
                    .Where(a => a.CustomerId == customer.Id && a.Id != address.Id && a.IsDefault)
                    .ToListAsync();
                foreach (var other in otherDefaults)
                {
                    other.IsDefault = false;
                }
                address.IsDefault = true;
            }

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Address updated successfully.");
        }

        public async Task<ServiceResult> SetDefaultAddressAsync(string userId, Guid addressId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.Fail("User not found.");

            var customer = await GetOrCreateCustomerAsync(user);
            if (customer == null) return ServiceResult.Fail("Customer not found.");

            var addresses = await _context.Addresses
                .Where(a => a.CustomerId == customer.Id)
                .ToListAsync();

            var target = addresses.FirstOrDefault(a => a.Id == addressId);
            if (target == null) return ServiceResult.Fail("Address not found.");

            foreach (var addr in addresses)
            {
                addr.IsDefault = (addr.Id == addressId);
            }

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Default shipping address set.");
        }

        public async Task<ServiceResult> DeleteAddressAsync(string userId, Guid addressId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.Fail("User not found.");

            var customer = await GetOrCreateCustomerAsync(user);
            if (customer == null) return ServiceResult.Fail("Customer not found.");

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customer.Id);

            if (address != null)
            {
                bool wasDefault = address.IsDefault;
                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();

                if (wasDefault)
                {
                    var nextAddress = await _context.Addresses
                        .FirstOrDefaultAsync(a => a.CustomerId == customer.Id);
                    if (nextAddress != null)
                    {
                        nextAddress.IsDefault = true;
                        await _context.SaveChangesAsync();
                    }
                }
                return ServiceResult.Ok("Address deleted successfully.");
            }

            return ServiceResult.Fail("Address not found.");
        }

        public async Task<ServiceResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.Fail("User not found.");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                return ServiceResult.Ok("Password changed successfully.");
            }

            return ServiceResult.Fail(result.Errors.Select(e => e.Description));
        }

        public async Task<ServiceResult> UploadAvatarAsync(string userId, IFormFile avatar, string webRootPath)
        {
            return await _avatarService.UploadAvatarAsync(userId, avatar);
        }

        public async Task<ServiceResult> RemoveAvatarAsync(string userId, string webRootPath)
        {
            return await _avatarService.DeleteAvatarAsync(userId);
        }

        public async Task<List<Address>> GetCustomerAddressesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<Address>();

            var customer = await GetOrCreateCustomerAsync(user);
            if (customer == null) return new List<Address>();

            return await _context.Addresses
                .AsNoTracking()
                .Where(a => a.CustomerId == customer.Id)
                .ToListAsync();
        }
    }
}
