using ClothingEcommerce.Client.Services.ApiClient;
using ClothingEcommerce.Shared.DTOs.Wishlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;

namespace ClothingEcommerce.Client.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly IApiClient _apiClient;

        public WishlistController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool IsAjaxRequest() =>
            Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
            Request.Headers.Accept.ToString().Contains("application/json");

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var response = await _apiClient.GetAsync<WishlistDto>("api/wishlist");
            var dto = response?.Data ?? new WishlistDto();

            var wishlist = new Wishlist
            {
                Id = dto.Id,
                CustomerId = dto.CustomerId,
                Items = dto.Items.Select(i => new WishlistItem
                {
                    Id = i.Id,
                    VariantId = i.VariantId,
                    WishlistId = dto.Id,
                    Variant = new ProductVariant
                    {
                        Id = i.VariantId,
                        ProductId = i.ProductId,
                        Price = i.Price,
                        Product = new Product
                        {
                            Id = i.ProductId,
                            Name = i.ProductName,
                            Images = !string.IsNullOrEmpty(i.ProductImage)
                                ? new List<ProductImage> { new() { ImageUrl = i.ProductImage, IsPrimary = true } }
                                : new List<ProductImage>()
                        },
                        Size = !string.IsNullOrEmpty(i.SizeName) ? new Size { Name = i.SizeName } : null,
                        Color = !string.IsNullOrEmpty(i.ColorName) ? new Color { Name = i.ColorName, HexCode = i.ColorHex ?? "#000000" } : null,
                        Inventory = new Inventory { Quantity = i.Stock, ReservedQuantity = 0 }
                    }
                }).ToList()
            };

            return View(wishlist);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Guid variantId)
        {
            var response = await _apiClient.PostAsync<WishlistActionRequestDto, object>("api/wishlist/add", new WishlistActionRequestDto { VariantId = variantId });
            var success = response?.Success ?? false;
            var message = response?.Message ?? (success ? "Item added to wishlist." : "Failed to add item to wishlist.");

            if (success) TempData["Success"] = message;
            else TempData["Error"] = message;

            if (IsAjaxRequest())
            {
                return Json(new { success, isWishlisted = success, message });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(Guid? id, Guid? variantId)
        {
            var response = await _apiClient.PostAsync<WishlistRemoveRequestDto, object>("api/wishlist/remove", new WishlistRemoveRequestDto { Id = id, VariantId = variantId });
            var success = response?.Success ?? false;
            var message = response?.Message ?? (success ? "Item removed from wishlist." : "Failed to remove item.");

            if (success) TempData["Success"] = message;
            else TempData["Error"] = message;

            if (IsAjaxRequest())
            {
                return Json(new { success, isWishlisted = false, message });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(Guid variantId)
        {
            var response = await _apiClient.PostAsync<WishlistActionRequestDto, dynamic>("api/wishlist/toggle", new WishlistActionRequestDto { VariantId = variantId });
            var success = response?.Success ?? false;
            var message = response?.Message ?? "Wishlist updated.";

            if (IsAjaxRequest())
            {
                return Json(new { success, message });
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveToCart(Guid wishlistItemId)
        {
            var response = await _apiClient.PostAsync<WishlistMoveRequestDto, object>("api/wishlist/move-to-cart", new WishlistMoveRequestDto { WishlistItemId = wishlistItemId });
            var success = response?.Success ?? false;
            var message = response?.Message ?? (success ? "Item moved to cart." : "Failed to move item to cart.");

            if (success) TempData["Success"] = message;
            else TempData["Error"] = message;

            if (IsAjaxRequest())
            {
                return Json(new { success, message });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var response = await _apiClient.PostAsync<object, object>("api/wishlist/clear", new { });
            var success = response?.Success ?? false;

            if (success) TempData["Success"] = "Wishlist cleared.";
            else TempData["Error"] = "Failed to clear wishlist.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            var response = await _apiClient.GetAsync<int>("api/wishlist/count");
            return Json(new { count = response?.Data ?? 0 });
        }
    }
}
