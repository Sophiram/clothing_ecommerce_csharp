using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class WishlistsController : Controller
    {
        private readonly IWishlistService _wishlistService;
        private readonly IAuditService _auditService;

        public WishlistsController(IWishlistService wishlistService, IAuditService auditService)
        {
            _wishlistService = wishlistService;
            _auditService = auditService;
        }

        // =========================================================
        // GET: /Admin/Wishlists
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? search = null)
        {
            var wishlists = await _wishlistService.GetAllWishlistsAsync(search);

            ViewBag.Search = search;
            ViewBag.TotalCount = wishlists.Count;
            ViewBag.TotalItemsSaved = wishlists.Sum(w => w.Items.Count);

            return View(wishlists);
        }

        // =========================================================
        // GET: /Admin/Wishlists/Details/{id}
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var wishlist = await _wishlistService.GetWishlistByIdAsync(id.Value);
            if (wishlist == null) return NotFound();

            return View(wishlist);
        }

        // =========================================================
        // POST: /Admin/Wishlists/Delete/{id}
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var wishlist = await _wishlistService.GetWishlistByIdAsync(id);
            if (wishlist == null) return NotFound();

            var success = await _wishlistService.DeleteWishlistAsync(id);
            if (success)
            {
                await _auditService.LogAsync(
                    User.Identity?.Name,
                    User.Identity?.Name,
                    "DeleteWishlist",
                    "Wishlist",
                    id.ToString(),
                    $"Deleted wishlist for customer {wishlist.Customer?.Email}",
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["Success"] = "Customer wishlist removed successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
