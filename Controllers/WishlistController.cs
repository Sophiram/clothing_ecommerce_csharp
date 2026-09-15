using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly IWishlistService _wishlistService;
        private readonly ICartService _cartService;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(
            IWishlistService wishlistService,
            ICartService cartService,
            UserManager<ApplicationUser> userManager)
        {
            _wishlistService = wishlistService;
            _cartService = cartService;
            _userManager = userManager;
        }

        private async Task<Customer?> GetCurrentCustomerAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _cartService.GetCustomerByUserIdOrEmailAsync(user.Id, user.Email);
        }

        private bool IsAjaxRequest() =>
            Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
            Request.Headers.Accept.ToString().Contains("application/json");

        // =====================================================
        // WISHLIST INDEX
        // GET: /Wishlist
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                TempData["Error"] = "Customer profile not found.";
                return RedirectToAction("Index", "Home");
            }

            var wishlist = await _wishlistService.GetOrCreateCustomerWishlistAsync(customer.Id);
            return View(wishlist);
        }

        // =====================================================
        // ADD TO WISHLIST
        // POST: /Wishlist/Add
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Guid variantId)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                if (IsAjaxRequest()) return Unauthorized();
                return RedirectToAction("Index", "Home");
            }

            var (success, message) = await _wishlistService.AddToWishlistAsync(customer.Id, variantId);

            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            if (IsAjaxRequest())
            {
                return Json(new { success, isWishlisted = success, message });
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // REMOVE FROM WISHLIST
        // POST: /Wishlist/Remove
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(Guid? id, Guid? variantId)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                if (IsAjaxRequest()) return Unauthorized();
                return RedirectToAction("Index", "Home");
            }

            var (success, message) = await _wishlistService.RemoveFromWishlistAsync(customer.Id, id, variantId);
            if (success)
            {
                TempData["Success"] = message;
            }

            if (IsAjaxRequest())
            {
                return Json(new { success, isWishlisted = false, message });
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // TOGGLE WISHLIST
        // POST: /Wishlist/Toggle
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(Guid variantId)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                if (IsAjaxRequest()) return Unauthorized();
                return RedirectToAction("Index", "Home");
            }

            var (success, isWishlisted, message) = await _wishlistService.ToggleWishlistAsync(customer.Id, variantId);
            TempData["Success"] = message;

            if (IsAjaxRequest())
            {
                return Json(new { success, isWishlisted, message });
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // MOVE WISHLIST ITEM TO CART
        // POST: /Wishlist/MoveToCart
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveToCart(Guid id)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                TempData["Error"] = "Customer profile not found.";
                return RedirectToAction("Index", "Home");
            }

            var (success, message) = await _wishlistService.MoveToCartAsync(customer.Id, id);

            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // CLEAR WISHLIST
        // POST: /Wishlist/Clear
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                TempData["Error"] = "Customer profile not found.";
                return RedirectToAction("Index", "Home");
            }

            await _wishlistService.ClearWishlistAsync(customer.Id);
            TempData["Success"] = "Your wishlist has been cleared.";

            return RedirectToAction(nameof(Index));
        }
    }
}