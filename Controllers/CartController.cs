using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IProductVariantService _productVariantService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(
            ICartService cartService,
            IProductVariantService productVariantService,
            UserManager<ApplicationUser> userManager)
        {
            _cartService = cartService;
            _productVariantService = productVariantService;
            _userManager = userManager;
        }

        private async Task<Customer?> GetCurrentCustomerAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _cartService.GetCustomerByUserIdOrEmailAsync(user.Id, user.Email);
        }

        // =========================================================
        // CART INDEX
        // GET: /Cart
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                TempData["Error"] = "Customer profile was not found.";
                return RedirectToAction("Index", "Home");
            }

            var model = await _cartService.GetCartAsync(customer.Id);
            return View(model);
        }

        // =========================================================
        // SIDEBAR PARTIAL (AJAX)
        // GET: /Cart/SidebarPartial
        // =========================================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult SidebarPartial()
        {
            return PartialView("_CartSidebar");
        }

        // =========================================================
        // ADD TO CART
        // POST: /Cart/Add
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Guid variantId, int quantity = 1)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var result = await _cartService.AddItemAsync(customer.Id, variantId, quantity);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    cartCount = result.CartCount
                });
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // GET CART COUNT (AJAX)
        // GET: /Cart/GetCount
        // =========================================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCount()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Json(new { count = 0 });
            }

            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Json(new { count = 0 });
            }

            var count = await _cartService.GetCartCountAsync(customer.Id);
            return Json(new { count });
        }

        // =========================================================
        // UPDATE CART ITEM QUANTITY
        // POST: /Cart/Update
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Guid id, int quantity, string? returnUrl = null)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                    Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = "Customer not authenticated." });
                }
                return RedirectToAction("Index", "Home");
            }

            var result = await _cartService.UpdateQuantityAsync(customer.Id, id, quantity);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    cartCount = result.CartCount
                });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // REMOVE FROM CART
        // POST: /Cart/Remove
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(Guid id, string? returnUrl = null)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                    Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = "Customer not authenticated." });
                }
                return RedirectToAction("Index", "Home");
            }

            var result = await _cartService.RemoveItemAsync(customer.Id, id);
            TempData["Success"] = result.Message;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    cartCount = result.CartCount
                });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // CHANGE CART ITEM VARIANT (SIZE/COLOR)
        // POST: /Cart/ChangeVariant
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeVariant(Guid id, Guid newVariantId, string? returnUrl = null)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                    Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = "Customer not authenticated." });
                }
                return RedirectToAction("Index", "Home");
            }

            var result = await _cartService.ChangeVariantAsync(customer.Id, id, newVariantId);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    cartCount = result.CartCount
                });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // GET PRODUCT VARIANTS (AJAX FOR VARIANT EDIT)
        // GET: /Cart/GetProductVariants?productId=...
        // =========================================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductVariants(Guid productId)
        {
            var variants = await _productVariantService.GetVariantsAsync(productId);
            var result = variants.Select(v => new
            {
                id = v.Id,
                productId = v.ProductId,
                sizeId = v.SizeId,
                sizeName = v.Size?.Name ?? "",
                colorId = v.ColorId,
                colorName = v.Color?.Name ?? "",
                colorHex = v.Color?.HexCode ?? "",
                price = v.Price,
                compareAtPrice = v.CompareAtPrice,
                stock = v.Inventory?.AvailableQuantity ?? 0,
                sku = v.SKU
            });

            return Json(result);
        }

        // =========================================================
        // CHECKOUT REDIRECT
        // =========================================================
        [HttpGet]
        public IActionResult Checkout()
        {
            return RedirectToAction("Index", "Checkout");
        }
    }
}