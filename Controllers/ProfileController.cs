using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =====================================================
        // PROFILE
        // GET: /Profile
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var customer = await _context.Customers
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c =>
                    c.ApplicationUserId == user.Id);

            if (customer == null)
            {
                TempData["Error"] = "Customer profile not found.";
                return RedirectToAction("Index", "Home");
            }

            var model = new ProfileViewModel
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Phone = customer.Phone,
                Addresses = customer.Addresses
            };

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
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.ApplicationUserId == user.Id);

            if (customer == null)
            {
                TempData["Error"] = "Customer profile not found.";
                return RedirectToAction("Index", "Home");
            }

            customer.FirstName = model.FirstName;
            customer.LastName = model.LastName;
            customer.Phone = model.Phone;

            // Optional:
            // Keep Identity user information synchronized
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.Phone;

            await _userManager.UpdateAsync(user);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // ADD ADDRESS
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(Address address)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.ApplicationUserId == user.Id);

            if (customer == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var hasAddress = await _context.Addresses
                .AnyAsync(a => a.CustomerId == customer.Id);

            address.Id = Guid.NewGuid();
            address.CustomerId = customer.Id;

            if (!hasAddress)
            {
                address.IsDefault = true;
            }

            _context.Addresses.Add(address);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Address added.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // DELETE ADDRESS
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.ApplicationUserId == user.Id);

            if (customer == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.CustomerId == customer.Id);

            if (address != null)
            {
                _context.Addresses.Remove(address);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Address deleted.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}