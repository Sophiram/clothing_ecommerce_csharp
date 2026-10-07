using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class WishlistItemsController : Controller
    {
        private readonly IWishlistService _wishlistService;

        public WishlistItemsController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        // GET: Admin/WishlistItems
        public async Task<IActionResult> Index()    
        {
            var items = await _wishlistService.GetAllWishlistItemsAsync();
            return View(items);
        }

        // GET: Admin/WishlistItems/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wishlistitem = await _wishlistService.GetWishlistItemByIdAsync(id.Value);
            if (wishlistitem == null)
            {
                return NotFound();
            }

            return View(wishlistitem);
        }

        // GET: Admin/WishlistItems/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/WishlistItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,WishlistId,VariantId,Wishlist,Variant")] WishlistItem wishlistitem)
        {
            if (ModelState.IsValid)
            {
                var result = await _wishlistService.CreateWishlistItemAsync(wishlistitem);
                if (result.Success)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, result.Message);
            }
            return View(wishlistitem);
        }

        // GET: Admin/WishlistItems/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wishlistitem = await _wishlistService.GetWishlistItemByIdAsync(id.Value);
            if (wishlistitem == null)
            {
                return NotFound();
            }
            return View(wishlistitem);
        }

        // POST: Admin/WishlistItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid? id, [Bind("Id,WishlistId,VariantId,Wishlist,Variant")] WishlistItem wishlistitem)
        {
            if (id == null || id != wishlistitem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var result = await _wishlistService.UpdateWishlistItemAsync(id.Value, wishlistitem);
                if (result.Success)
                {
                    return RedirectToAction(nameof(Index));
                }
                if (!await _wishlistService.WishlistItemExistsAsync(wishlistitem.Id))
                {
                    return NotFound();
                }
                ModelState.AddModelError(string.Empty, result.Message);
            }
            return View(wishlistitem);
        }

        // GET: Admin/WishlistItems/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wishlistitem = await _wishlistService.GetWishlistItemByIdAsync(id.Value);
            if (wishlistitem == null)
            {
                return NotFound();
            }

            return View(wishlistitem);
        }

        // POST: Admin/WishlistItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid? id)
        {
            if (id.HasValue)
            {
                await _wishlistService.DeleteWishlistItemAsync(id.Value);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
