using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =====================================================
        // GET CUSTOMER
        // =====================================================

        private async Task<Customer?> GetCustomer()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            return await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.ApplicationUserId == user.Id);
        }


        // =====================================================
        // WISHLIST INDEX
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customer = await GetCustomer();

            if (customer == null)
            {
                TempData["Error"] = "Customer profile not found.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }


            var wishlist = await _context.Wishlists

                .Include(w => w.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)

                .Include(w => w.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Size)

                .Include(w => w.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Color)

                .FirstOrDefaultAsync(w =>
                    w.CustomerId == customer.Id);


            // Create wishlist if user doesn't have one
            if (wishlist == null)
            {
                wishlist = new Wishlist
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id
                };

                _context.Wishlists.Add(wishlist);

                await _context.SaveChangesAsync();
            }


            return View(wishlist);
        }


        // =====================================================
        // ADD TO WISHLIST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Guid variantId)
        {
            var customer = await GetCustomer();

            if (customer == null)
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            var variant = await _context.ProductVariants
                .FirstOrDefaultAsync(v =>
                    v.Id == variantId);

            if (variant == null)
            {
                return NotFound();
            }


            var wishlist = await _context.Wishlists
                .FirstOrDefaultAsync(w =>
                    w.CustomerId == customer.Id);


            if (wishlist == null)
            {
                wishlist = new Wishlist
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id
                };

                _context.Wishlists.Add(wishlist);

                await _context.SaveChangesAsync();
            }


            var exists = await _context.WishlistItems
                .AnyAsync(i =>
                    i.WishlistId == wishlist.Id &&
                    i.VariantId == variantId);


            if (!exists)
            {
                var item = new WishlistItem
                {
                    Id = Guid.NewGuid(),
                    WishlistId = wishlist.Id,
                    VariantId = variantId
                };

                _context.WishlistItems.Add(item);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Product added to wishlist.";
            }
            else
            {
                TempData["Error"] =
                    "Product is already in your wishlist.";
            }


            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // REMOVE
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(Guid id)
        {
            var customer = await GetCustomer();

            if (customer == null)
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            var item = await _context.WishlistItems

                .Include(i => i.Wishlist)

                .FirstOrDefaultAsync(i =>
                    i.Id == id &&
                    i.Wishlist.CustomerId == customer.Id);


            if (item != null)
            {
                _context.WishlistItems.Remove(item);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Product removed from wishlist.";
            }


            return RedirectToAction(nameof(Index));
        }
    }
}