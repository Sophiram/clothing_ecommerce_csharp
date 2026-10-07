using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [Route("Admin/[controller]/[action]/{id?}")]
    [Route("Admin/CartItem/[action]/{id?}")]
    [Route("Admin/[controller]")]
    [Route("Admin/CartItem")]
    public class CartItemsController : Controller
    {
        private readonly ICartService _cartService;

        public CartItemsController(ICartService cartService)
        {
            _cartService = cartService;
        }

        // GET: /Admin/CartItems or /Admin/CartItem
        [HttpGet]
        public async Task<IActionResult> Index()    
        {
            var items = await _cartService.GetAllCartItemsAsync();
            return View("~/Areas/Admin/Views/CartItem/Index.cshtml", items);
        }

        // GET: /Admin/CartItems/Details/5 or /Admin/CartItem/Details
        [HttpGet]
        public async Task<IActionResult> Details(Guid? id)
        {
            CartItem? cartitem = null;

            if (id.HasValue && id.Value != Guid.Empty)
            {
                cartitem = await _cartService.GetCartItemByIdAsync(id.Value);
            }

            if (cartitem == null)
            {
                // Fallback to first available cart item in database
                cartitem = await _cartService.GetFirstCartItemAsync();
            }

            if (cartitem == null)
            {
                // If cart items table is completely empty, auto-seed sample cart items
                cartitem = await _cartService.EnsureSampleCartItemsAsync();
            }

            return View("~/Areas/Admin/Views/CartItem/Details.cshtml", cartitem);
        }

        // POST: /Admin/CartItems/SeedSample
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedSample()
        {
            var item = await _cartService.EnsureSampleCartItemsAsync();
            if (item != null)
            {
                TempData["Success"] = "Sample cart item has been populated successfully.";
                return RedirectToAction(nameof(Details), new { id = item.Id });
            }
            TempData["Error"] = "Unable to create sample cart item. Please ensure products exist in the catalog.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/CartItems/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View("~/Areas/Admin/Views/CartItem/Create.cshtml");
        }

        // POST: /Admin/CartItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CartId,VariantId,Quantity")] CartItem cartitem)
        {
            if (ModelState.IsValid)
            {
                await _cartService.CreateCartItemAsync(cartitem);
                return RedirectToAction(nameof(Index));
            }
            return View("~/Areas/Admin/Views/CartItem/Create.cshtml", cartitem);
        }

        // GET: /Admin/CartItems/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cartitem = await _cartService.GetCartItemByIdAsync(id.Value);
            if (cartitem == null)
            {
                return NotFound();
            }
            return View("~/Areas/Admin/Views/CartItem/Edit.cshtml", cartitem);
        }

        // POST: /Admin/CartItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,CartId,VariantId,Quantity")] CartItem cartitem)
        {
            if (id != cartitem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var result = await _cartService.UpdateCartItemAsync(id, cartitem);
                if (!result.Success)
                {
                    return NotFound();
                }
                return RedirectToAction(nameof(Index));
            }
            return View("~/Areas/Admin/Views/CartItem/Edit.cshtml", cartitem);
        }

        // GET: /Admin/CartItems/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cartitem = await _cartService.GetCartItemByIdAsync(id.Value);
            if (cartitem == null)
            {
                return NotFound();
            }

            return View("~/Areas/Admin/Views/CartItem/Delete.cshtml", cartitem);
        }

        // POST: /Admin/CartItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _cartService.DeleteCartItemAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
