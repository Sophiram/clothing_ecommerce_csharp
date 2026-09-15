using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Services
{
    public class AdminUserService : IAdminUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditService _auditService;
        private readonly IWebHostEnvironment _environment;

        private static readonly HashSet<string> SystemRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "SuperAdmin",
            "Admin",
            "User"
        };

        public AdminUserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IAuditService auditService,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
            _environment = environment;
        }

        private async Task EnsureRolesExistAsync()
        {
            if (!await _roleManager.RoleExistsAsync("SuperAdmin"))
                await _roleManager.CreateAsync(new IdentityRole("SuperAdmin"));

            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));

            if (!await _roleManager.RoleExistsAsync("User"))
                await _roleManager.CreateAsync(new IdentityRole("User"));
        }

        private string? GetAvatarUrl(string userId)
        {
            var folder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
            if (!Directory.Exists(folder)) return null;

            var files = Directory.GetFiles(folder, $"{userId}.*");
            if (files.Length > 0)
            {
                var fi = new FileInfo(files[0]);
                return $"/uploads/avatars/{Path.GetFileName(files[0])}?v={fi.LastWriteTimeUtc.Ticks}";
            }
            return null;
        }

        private void DeleteAvatar(string userId)
        {
            try
            {
                var folder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                if (Directory.Exists(folder))
                {
                    var files = Directory.GetFiles(folder, $"{userId}.*");
                    foreach (var f in files)
                    {
                        File.Delete(f);
                    }
                }
            }
            catch { }
        }

        // =====================================================
        // ADMINS
        // =====================================================
        public async Task<List<AdminListItemViewModel>> GetAdminsAsync(string? search, string? roleFilter, string currentUserId)
        {
            await EnsureRolesExistAsync();

            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var superAdminUsers = await _userManager.GetUsersInRoleAsync("SuperAdmin");

            var allAdminMap = new Dictionary<string, ApplicationUser>();
            foreach (var u in adminUsers) allAdminMap[u.Id] = u;
            foreach (var u in superAdminUsers) allAdminMap[u.Id] = u;

            var query = allAdminMap.Values.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(u =>
                    (u.FirstName != null && u.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (u.LastName != null && u.LastName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (u.Email != null && u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase))
                );
            }

            var adminList = new List<AdminListItemViewModel>();

            foreach (var user in query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName))
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (!string.IsNullOrWhiteSpace(roleFilter))
                {
                    if (!roles.Contains(roleFilter)) continue;
                }

                var isLockedOut = await _userManager.IsLockedOutAsync(user);
                var avatarUrl = GetAvatarUrl(user.Id);

                adminList.Add(new AdminListItemViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    IsActive = !isLockedOut,
                    IsCurrentAdmin = (user.Id == currentUserId),
                    AvatarUrl = avatarUrl,
                    Roles = roles
                });
            }

            return adminList;
        }

        public async Task<(int Total, int SuperAdmins, int Admins)> GetAdminCountsAsync()
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var superAdminUsers = await _userManager.GetUsersInRoleAsync("SuperAdmin");

            var allAdminMap = new Dictionary<string, ApplicationUser>();
            foreach (var u in adminUsers) allAdminMap[u.Id] = u;
            foreach (var u in superAdminUsers) allAdminMap[u.Id] = u;

            return (allAdminMap.Count, superAdminUsers.Count, adminUsers.Count);
        }

        public async Task<AdminDetailsViewModel?> GetAdminDetailsAsync(string id, string currentUserId)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            var isLockedOut = await _userManager.IsLockedOutAsync(user);

            return new AdminDetailsViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                IsActive = !isLockedOut,
                IsCurrentAdmin = (user.Id == currentUserId),
                AvatarUrl = GetAvatarUrl(user.Id),
                Roles = roles,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount
            };
        }

        public async Task<AdminEditViewModel?> GetAdminForEditAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            var isLockedOut = await _userManager.IsLockedOutAsync(user);

            return new AdminEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                IsActive = !isLockedOut,
                Role = roles.Contains("SuperAdmin") ? "SuperAdmin" : "Admin"
            };
        }

        public async Task<ServiceResult> CreateAdminAsync(AdminCreateViewModel model, string? currentUserId, string? currentEmail, string? ip)
        {
            await EnsureRolesExistAsync();

            var existingUser = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (existingUser != null)
            {
                return ServiceResult.Fail($"A user with email '{model.Email}' already exists.");
            }

            var user = new ApplicationUser
            {
                UserName = model.Email.Trim(),
                Email = model.Email.Trim(),
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                PhoneNumber = model.PhoneNumber?.Trim(),
                EmailConfirmed = true,
                LockoutEnabled = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return ServiceResult.Fail(result.Errors.Select(e => e.Description));
            }

            var assignedRole = (model.Role == "SuperAdmin") ? "SuperAdmin" : "Admin";
            await _userManager.AddToRoleAsync(user, assignedRole);

            await _auditService.LogAsync(
                currentUserId,
                currentEmail,
                "CreateAdmin",
                "User",
                user.Id,
                $"Created new {assignedRole} account: '{user.FullName}' ({user.Email})",
                ip
            );

            return ServiceResult.Ok($"{assignedRole} '{user.FullName}' ({user.Email}) created successfully!");
        }

        public async Task<ServiceResult> UpdateAdminAsync(AdminEditViewModel model, string currentUserId, string? currentEmail, string? ip)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return ServiceResult.Fail("Administrator account not found.");
            }

            var isSelf = (user.Id == currentUserId);

            if (isSelf && !model.IsActive)
            {
                return ServiceResult.Fail("Security Protection: You cannot deactivate your own administrative account.");
            }

            if (!string.Equals(user.Email, model.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _userManager.FindByEmailAsync(model.Email.Trim());
                if (existing != null && existing.Id != user.Id)
                {
                    return ServiceResult.Fail($"Email '{model.Email}' is already registered to another account.");
                }
                user.Email = model.Email.Trim();
                user.UserName = model.Email.Trim();
            }

            user.FirstName = model.FirstName.Trim();
            user.LastName = model.LastName.Trim();
            user.PhoneNumber = model.PhoneNumber?.Trim();
            user.EmailConfirmed = model.EmailConfirmed;

            user.LockoutEnabled = true;
            if (model.IsActive)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var targetRole = (model.Role == "SuperAdmin") ? "SuperAdmin" : "Admin";

            if (currentRoles.Contains("SuperAdmin") && targetRole != "SuperAdmin")
            {
                return ServiceResult.Fail("Security Protection: SuperAdmin accounts cannot be downgraded from the normal Admin management UI. Manage platform roles in the Users module.");
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return ServiceResult.Fail(result.Errors.Select(e => e.Description));
            }

            if (!currentRoles.Contains(targetRole))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, targetRole);
            }

            await _auditService.LogAsync(
                currentUserId,
                currentEmail,
                "EditAdmin",
                "User",
                user.Id,
                $"Updated administrator '{user.FullName}' ({user.Email}) - Role: {targetRole}, Active: {model.IsActive}",
                ip
            );

            return ServiceResult.Ok($"Administrator '{user.FullName}' updated successfully.");
        }

        public async Task<ServiceResult> ToggleAdminStatusAsync(string id, string currentUserId, string? currentEmail, string? ip)
        {
            if (string.Equals(id, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail("Security Protection: You cannot deactivate your own administrator account.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return ServiceResult.Fail("Administrator account not found.");
            }

            var isLockedOut = await _userManager.IsLockedOutAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            if (!isLockedOut && roles.Contains("SuperAdmin"))
            {
                var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
                var activeSuperAdmins = 0;
                foreach (var sa in superAdmins)
                {
                    if (!await _userManager.IsLockedOutAsync(sa)) activeSuperAdmins++;
                }
                if (activeSuperAdmins <= 1)
                {
                    return ServiceResult.Fail("Security Protection: Cannot deactivate the only active SuperAdmin account.");
                }
            }

            user.LockoutEnabled = true;
            string actionText;
            if (isLockedOut)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                actionText = "Activated";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                actionText = "Deactivated";
            }

            await _auditService.LogAsync(
                currentUserId,
                currentEmail,
                $"{actionText}Admin",
                "User",
                user.Id,
                $"{actionText} administrator account '{user.FullName}' ({user.Email})",
                ip
            );

            return ServiceResult.Ok($"Administrator '{user.FullName}' account has been {actionText.ToLowerInvariant()}.");
        }

        public async Task<ServiceResult> ResetAdminPasswordAsync(string id, string newPassword, string? currentUserId, string? currentEmail, string? ip)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return ServiceResult.Fail("Administrator account not found.");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                return ServiceResult.Fail(result.Errors.Select(e => e.Description));
            }

            await _auditService.LogAsync(
                currentUserId,
                currentEmail,
                "ResetAdminPassword",
                "User",
                user.Id,
                $"Reset password for administrator '{user.FullName}' ({user.Email})",
                ip
            );

            return ServiceResult.Ok($"Password for '{user.FullName}' was successfully reset.");
        }

        public async Task<ServiceResult> DeleteAdminAsync(string id, string currentUserId, string? currentEmail, string? ip)
        {
            if (string.Equals(id, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail("Security Protection: You cannot delete your own administrator account.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return ServiceResult.Fail("Administrator account not found.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("SuperAdmin"))
            {
                return ServiceResult.Fail("Security Protection: SuperAdmin accounts cannot be deleted from the normal Admin management UI. Manage platform accounts in the Users module.");
            }

            var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");
            var allSuperAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            var totalPrivileged = allAdmins.Union(allSuperAdmins).Select(u => u.Id).Distinct().Count();
            if (totalPrivileged <= 1)
            {
                return ServiceResult.Fail("Security Protection: Cannot delete the last administrator in the system.");
            }

            DeleteAvatar(user.Id);

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return ServiceResult.Fail(result.Errors.Select(e => e.Description));
            }

            await _auditService.LogAsync(
                currentUserId,
                currentEmail,
                "DeleteAdmin",
                "User",
                user.Id,
                $"Permanently deleted administrator '{user.FullName}' ({user.Email})",
                ip
            );

            return ServiceResult.Ok($"Administrator '{user.FullName}' ({user.Email}) was permanently deleted.");
        }

        public async Task<ServiceResult> RevokeAdminRoleAsync(string id, string currentUserId, string? currentEmail, string? ip)
        {
            if (string.Equals(id, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail("Security Protection: You cannot revoke administrative privileges from your own account.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return ServiceResult.Fail("Account not found.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("SuperAdmin"))
            {
                return ServiceResult.Fail("Security Protection: SuperAdmin accounts cannot have their administrative roles revoked from the normal Admin management UI.");
            }

            if (roles.Contains("Admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");

                if (!await _roleManager.RoleExistsAsync("User"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("User"));
                }

                if (!await _userManager.IsInRoleAsync(user, "User"))
                {
                    await _userManager.AddToRoleAsync(user, "User");
                }

                await _auditService.LogAsync(
                    currentUserId,
                    currentEmail,
                    "RevokeAdminRole",
                    "User",
                    user.Id,
                    $"Revoked Admin role from '{user.FullName}' ({user.Email}), demoted to standard User",
                    ip
                );

                return ServiceResult.Ok($"Administrative privileges removed for '{user.FullName}'. Account has been demoted to standard User.");
            }

            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> AssignAdminRoleAsync(string email, string role, string? currentUserId, string? currentEmail, string? ip)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return ServiceResult.Fail("Please specify an email address.");
            }

            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user == null)
            {
                return ServiceResult.Fail($"No user found with email '{email.Trim()}'.");
            }

            var targetRole = (role == "SuperAdmin") ? "SuperAdmin" : "Admin";
            await EnsureRolesExistAsync();

            if (await _userManager.IsInRoleAsync(user, targetRole))
            {
                return ServiceResult.Ok($"User '{user.FullName}' ({user.Email}) is already an {targetRole}.");
            }

            await _userManager.AddToRoleAsync(user, targetRole);

            await _auditService.LogAsync(
                currentUserId,
                currentEmail,
                "AssignAdminRole",
                "User",
                user.Id,
                $"Assigned {targetRole} role to '{user.FullName}' ({user.Email})",
                ip
            );

            return ServiceResult.Ok($"User '{user.FullName}' ({user.Email}) has been successfully granted the {targetRole} role.");
        }

        // =====================================================
        // USERS
        // =====================================================
        public async Task<(List<ApplicationUser> Users, Dictionary<string, IList<string>> UserRoles, Dictionary<string, bool> UserLockouts, Dictionary<string, string?> UserAvatars, List<string> AllRoles)> GetUsersAsync(string? search, string? roleFilter)
        {
            var usersQuery = _userManager.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                usersQuery = usersQuery.Where(u =>
                    (u.FirstName != null && u.FirstName.Contains(search)) ||
                    (u.LastName != null && u.LastName.Contains(search)) ||
                    (u.Email != null && u.Email.Contains(search)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search))
                );
            }

            var users = await usersQuery.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();
            var userRolesMap = new Dictionary<string, IList<string>>();
            var userLockoutMap = new Dictionary<string, bool>();
            var userAvatarsMap = new Dictionary<string, string?>();
            var filteredUsers = new List<ApplicationUser>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (!string.IsNullOrWhiteSpace(roleFilter))
                {
                    if (!roles.Contains(roleFilter)) continue;
                }
                userRolesMap[user.Id] = roles;
                userLockoutMap[user.Id] = await _userManager.IsLockedOutAsync(user);
                userAvatarsMap[user.Id] = GetAvatarUrl(user.Id);
                filteredUsers.Add(user);
            }

            var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();

            return (filteredUsers, userRolesMap, userLockoutMap, userAvatarsMap, allRoles);
        }

        public async Task<(ApplicationUser? User, IList<string> Roles)> GetUserDetailsAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return (null, new List<string>());

            var roles = await _userManager.GetRolesAsync(user);
            return (user, roles);
        }

        public async Task<bool> GetUserLockoutStatusAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return false;
            return await _userManager.IsLockedOutAsync(user);
        }

        public async Task<List<string>> GetAllRoleNamesAsync()
        {
            return await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        }

        public async Task<ServiceResult> CreateUserAsync(string firstName, string lastName, string email, string password, string role, string? currentUserId, string? currentEmail, string? ip)
        {
            var user = new ApplicationUser
            {
                UserName = email.Trim(),
                Email = email.Trim(),
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return ServiceResult.Fail(result.Errors.Select(e => e.Description));
            }

            if (!string.IsNullOrEmpty(role) && await _roleManager.RoleExistsAsync(role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            await _auditService.LogAsync(
                currentUserId,
                currentEmail,
                "CreateUser",
                "User",
                user.Id,
                $"Created user '{user.FullName}' ({user.Email}) with role '{role}'",
                ip
            );

            return ServiceResult.Ok($"User '{user.FullName}' created successfully.");
        }

        public async Task<(ApplicationUser? User, IList<string> Roles)> GetUserForEditAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return (null, new List<string>());

            var roles = await _userManager.GetRolesAsync(user);
            return (user, roles);
        }

        public async Task<ServiceResult> UpdateUserAsync(string id, string firstName, string lastName, string email, string? phoneNumber, string role, string currentUserId, string? currentEmail, string? ip)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return ServiceResult.Fail("User not found.");

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (currentRoles.Contains("SuperAdmin") && role != "SuperAdmin")
            {
                var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
                if (superAdmins.Count <= 1)
                {
                    return ServiceResult.Fail("Security Protection: Cannot remove SuperAdmin role from the last remaining SuperAdmin.");
                }
            }

            user.FirstName = firstName.Trim();
            user.LastName = lastName.Trim();
            user.Email = email.Trim();
            user.UserName = email.Trim();
            user.PhoneNumber = phoneNumber?.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return ServiceResult.Fail(result.Errors.Select(e => e.Description));
            }

            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!string.IsNullOrEmpty(role) && await _roleManager.RoleExistsAsync(role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            await _auditService.LogAsync(
                currentUserId,
                currentEmail,
                "EditUser",
                "User",
                user.Id,
                $"Updated user '{user.FullName}' ({user.Email}) - assigned role '{role}'",
                ip
            );

            return ServiceResult.Ok($"User '{user.FullName}' updated successfully.");
        }

        public async Task<ServiceResult> DeleteUserAsync(string id, string currentUserId, string? currentEmail, string? ip)
        {
            if (string.Equals(id, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail("Security Protection: You cannot delete your own account.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return ServiceResult.Fail("User not found.");

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("SuperAdmin"))
            {
                var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
                if (superAdmins.Count <= 1)
                {
                    return ServiceResult.Fail("Security Protection: Cannot delete the only remaining SuperAdmin in the system.");
                }
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return ServiceResult.Fail(result.Errors.Select(e => e.Description));
            }

            await _auditService.LogAsync(
                currentUserId,
                currentEmail,
                "DeleteUser",
                "User",
                user.Id,
                $"Deleted user '{user.FullName}' ({user.Email})",
                ip
            );

            return ServiceResult.Ok($"User '{user.FullName}' was deleted successfully.");
        }

        public async Task<ServiceResult> ToggleUserStatusAsync(string id, string currentUserId, string? currentEmail, string? ip)
        {
            if (string.Equals(id, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail("Security Protection: You cannot deactivate your own account.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return ServiceResult.Fail("User not found.");

            var roles = await _userManager.GetRolesAsync(user);
            var isLockedOut = await _userManager.IsLockedOutAsync(user);

            if (!isLockedOut && roles.Contains("SuperAdmin"))
            {
                var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
                var activeSuperAdmins = 0;
                foreach (var sa in superAdmins)
                {
                    if (!await _userManager.IsLockedOutAsync(sa)) activeSuperAdmins++;
                }
                if (activeSuperAdmins <= 1)
                {
                    return ServiceResult.Fail("Security Protection: Cannot deactivate the only active SuperAdmin in the system.");
                }
            }

            user.LockoutEnabled = true;
            string actionText;
            if (isLockedOut)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                actionText = "Activated";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                actionText = "Deactivated";
            }

            await _auditService.LogAsync(
                currentUserId,
                currentEmail,
                $"{actionText}User",
                "User",
                user.Id,
                $"{actionText} user account '{user.FullName}' ({user.Email})",
                ip
            );

            return ServiceResult.Ok($"User '{user.FullName}' account has been {actionText.ToLowerInvariant()}.");
        }

        public async Task<ServiceResult> ResetUserPasswordAsync(string id, string newPassword, string confirmPassword, string? currentUserId, string? currentEmail, string? ip)
        {
            if (string.IsNullOrEmpty(newPassword))
            {
                return ServiceResult.Fail("Password cannot be empty.");
            }

            if (newPassword != confirmPassword)
            {
                return ServiceResult.Fail("Passwords do not match.");
            }

            if (newPassword.Length < 6)
            {
                return ServiceResult.Fail("Password must be at least 6 characters long.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return ServiceResult.Fail("User not found.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                return ServiceResult.Fail(result.Errors.Select(e => e.Description));
            }

            await _auditService.LogAsync(
                currentUserId,
                currentEmail,
                "ResetUserPassword",
                "User",
                user.Id,
                $"Reset password for user '{user.FullName}' ({user.Email})",
                ip
            );

            return ServiceResult.Ok($"Password for user '{user.FullName}' was reset successfully.");
        }

        // =====================================================
        // ROLES
        // =====================================================
        public async Task<(List<IdentityRole> Roles, Dictionary<string, int> UserCounts, HashSet<string> SystemRoles)> GetRolesAsync()
        {
            var roles = await _roleManager.Roles.AsNoTracking().ToListAsync();
            var roleUserCounts = new Dictionary<string, int>();

            foreach (var role in roles)
            {
                if (!string.IsNullOrEmpty(role.Name))
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
                    roleUserCounts[role.Id] = usersInRole.Count;
                }
                else
                {
                    roleUserCounts[role.Id] = 0;
                }
            }

            return (roles, roleUserCounts, SystemRoles);
        }

        public async Task<ServiceResult> CreateRoleAsync(string roleName, string? currentUserId, string? currentEmail, string? ip)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return ServiceResult.Fail("Role name cannot be empty.");
            }

            roleName = roleName.Trim();
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                return ServiceResult.Fail($"Role '{roleName}' already exists.");
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                return ServiceResult.Fail(result.Errors.Select(e => e.Description));
            }

            await _auditService.LogAsync(
                currentUserId,
                currentEmail,
                "CreateRole",
                "Role",
                roleName,
                $"Created new security role '{roleName}'",
                ip
            );

            return ServiceResult.Ok($"Role '{roleName}' created successfully.");
        }

        public async Task<ServiceResult> DeleteRoleAsync(string id, string? currentUserId, string? currentEmail, string? ip)
        {
            if (string.IsNullOrEmpty(id))
            {
                return ServiceResult.Fail("Invalid role ID.");
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return ServiceResult.Fail("Role not found.");
            }

            if (role.Name != null && SystemRoles.Contains(role.Name))
            {
                return ServiceResult.Fail($"Security Protection: Core system role '{role.Name}' cannot be deleted.");
            }

            if (role.Name != null)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
                if (usersInRole.Count > 0)
                {
                    return ServiceResult.Fail($"Cannot delete role '{role.Name}' because it is assigned to {usersInRole.Count} user(s). Reassign them first.");
                }
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                return ServiceResult.Fail(result.Errors.Select(e => e.Description));
            }

            await _auditService.LogAsync(
                currentUserId,
                currentEmail,
                "DeleteRole",
                "Role",
                role.Id,
                $"Deleted custom role '{role.Name}'",
                ip
            );

            return ServiceResult.Ok($"Role '{role.Name}' was deleted successfully.");
        }

        public async Task<ServiceResult> EditRoleAsync(string id, string newName, string? currentUserId, string? currentEmail, string? ip)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrWhiteSpace(newName))
            {
                return ServiceResult.Fail("Role name cannot be empty.");
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return ServiceResult.Fail("Role not found.");
            }

            if (role.Name != null && SystemRoles.Contains(role.Name))
            {
                return ServiceResult.Fail($"Security Protection: Core system role '{role.Name}' cannot be renamed.");
            }

            newName = newName.Trim();
            if (await _roleManager.RoleExistsAsync(newName))
            {
                return ServiceResult.Fail($"Role '{newName}' already exists.");
            }

            var oldName = role.Name;
            role.Name = newName;
            var result = await _roleManager.UpdateAsync(role);

            if (!result.Succeeded)
            {
                return ServiceResult.Fail(result.Errors.Select(e => e.Description));
            }

            await _auditService.LogAsync(
                currentUserId,
                currentEmail,
                "EditRole",
                "Role",
                role.Id,
                $"Renamed custom role from '{oldName}' to '{newName}'",
                ip
            );

            return ServiceResult.Ok($"Role '{oldName}' renamed to '{newName}' successfully.");
        }

        public async Task<(IdentityRole? Role, IList<ApplicationUser> Users, bool IsSystem)> GetUsersInRoleAsync(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName.Trim());
            if (role == null) return (null, new List<ApplicationUser>(), false);

            var users = await _userManager.GetUsersInRoleAsync(roleName.Trim());
            var isSystem = SystemRoles.Contains(role.Name ?? "");
            return (role, users, isSystem);
        }

        // =====================================================
        // SETTINGS
        // =====================================================
        public async Task SaveSettingsAsync(string storeName, string supportEmail, string supportPhone, string currency, bool maintenanceMode, bool allowRegistration, string? currentUserId, string? currentEmail, string? ip)
        {
            await _auditService.LogAsync(
                currentUserId,
                currentEmail,
                "UpdateSettings",
                "SystemSettings",
                "General",
                $"Updated settings: Store={storeName}, SupportEmail={supportEmail}, Currency={currency}, Maintenance={maintenanceMode}, Registration={allowRegistration}",
                ip
            );
        }
    }
}
