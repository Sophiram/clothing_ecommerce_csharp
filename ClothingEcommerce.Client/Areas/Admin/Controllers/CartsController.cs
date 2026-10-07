using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [Route("Admin/[controller]/[action]/{id?}")]
    [Route("Admin/Carts/[action]/{id?}")]
    [Route("Admin/[controller]")]
    [Route("Admin/Carts")]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        // GET: /Admin/Cart or /Admin/Carts
        [HttpGet]
        public async Task<IActionResult> Index(Guid? customerId)
        {
            Guid targetCustomerId = customerId ?? Guid.Empty;

            if (targetCustomerId == Guid.Empty)
            {
                var firstItem = await _cartService.GetFirstCartItemAsync();
                if (firstItem?.Cart?.CustomerId != null)
                {
                    targetCustomerId = firstItem.Cart.CustomerId;
                }
                else
                {
                    var sample = await _cartService.EnsureSampleCartItemsAsync();
                    if (sample?.Cart?.CustomerId != null)
                    {
                        targetCustomerId = sample.Cart.CustomerId;
                    }
                }
            }

            ViewBag.CustomerId = targetCustomerId;
            var cart = await _cartService.GetAdminCustomerCartAsync(targetCustomerId);
            return View("~/Areas/Admin/Views/Cart/Index.cshtml", cart);
        }

        // GET: /Admin/Cart/Details or /Admin/Carts/Details
        [HttpGet]
        public async Task<IActionResult> Details(Guid? id, Guid? customerId)
        {
            Guid targetCustomerId = customerId ?? Guid.Empty;

            if (targetCustomerId == Guid.Empty && id.HasValue && id.Value != Guid.Empty)
            {
                var cartById = await _cartService.GetAdminCustomerCartAsync(id.Value);
                if (cartById != null)
                {
                    ViewBag.CustomerId = cartById.CustomerId;
                    return View("~/Areas/Admin/Views/Cart/Details.cshtml", cartById);
                }
                targetCustomerId = id.Value;
            }

            if (targetCustomerId == Guid.Empty)
            {
                var firstItem = await _cartService.GetFirstCartItemAsync();
                if (firstItem?.Cart?.CustomerId != null)
                {
                    targetCustomerId = firstItem.Cart.CustomerId;
                }
                else
                {
                    var sample = await _cartService.EnsureSampleCartItemsAsync();
                    if (sample?.Cart?.CustomerId != null)
                    {
                        targetCustomerId = sample.Cart.CustomerId;
                    }
                }
            }

            ViewBag.CustomerId = targetCustomerId;
            var cart = await _cartService.GetAdminCustomerCartAsync(targetCustomerId);
            return View("~/Areas/Admin/Views/Cart/Details.cshtml", cart);
        }

        // POST: /Admin/Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(Guid customerId, Guid variantId, int quantity = 1)
        {
            if (quantity <= 0) quantity = 1;
            await _cartService.AddItemAsync(customerId, variantId, quantity);
            return RedirectToAction(nameof(Index), new { customerId });
        }

        // POST: /Admin/Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(Guid customerId, Guid cartItemId, int quantity)
        {
            await _cartService.UpdateQuantityAsync(customerId, cartItemId, quantity);
            return RedirectToAction(nameof(Index), new { customerId });
        }

        // POST: /Admin/Cart/RemoveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(Guid customerId, Guid cartItemId)
        {
            await _cartService.RemoveItemAsync(customerId, cartItemId);
            return RedirectToAction(nameof(Index), new { customerId });
        }

        // POST: /Admin/Cart/ClearCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart(Guid customerId)
        {
            await _cartService.ClearCartAsync(customerId);
            return RedirectToAction(nameof(Index), new { customerId });
        }

        // POST: /Admin/Cart/SeedSample
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedSample(Guid? customerId)
        {
            var item = await _cartService.EnsureSampleCartItemsAsync(customerId);
            TempData["Success"] = "Sample cart items have been populated successfully.";
            return RedirectToAction(nameof(Index), new { customerId = item?.Cart?.CustomerId ?? customerId });
        }
    }
}