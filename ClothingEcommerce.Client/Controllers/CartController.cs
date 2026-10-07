using ClothingEcommerce.Client.Services.ApiClient;
using ClothingEcommerce.Shared.DTOs.Catalog;
using ClothingEcommerce.Shared.DTOs.Orders;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace ClothingEcommerce.Client.Controllers
{
    public class CartController : Controller
    {
        private readonly IApiClient _apiClient;

        public CartController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool IsAjaxRequest() =>
            Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
            Request.Headers.Accept.ToString().Contains("application/json");

        private CartViewModel MapDtoToViewModel(CartDto dto)
        {
            var cart = new Cart
            {
                Id = dto.Id,
                CustomerId = Guid.Empty,
                CreatedAt = DateTime.UtcNow,
                Items = dto.Items.Select(i => new CartItem
                {
                    Id = i.Id,
                    CartId = dto.Id,
                    VariantId = i.VariantId ?? Guid.Empty,
                    Quantity = i.Quantity,
                    Variant = new ProductVariant
                    {
                        Id = i.VariantId ?? Guid.Empty,
                        ProductId = i.ProductId,
                        Price = i.UnitPrice,
                        SKU = i.VariantSku ?? "",
                        Product = new Product
                        {
                            Id = i.ProductId,
                            Name = i.ProductName,
                            Images = !string.IsNullOrEmpty(i.ProductImage)
                                ? new List<ProductImage> { new() { ImageUrl = i.ProductImage, IsPrimary = true } }
                                : new List<ProductImage>()
                        },
                        Size = !string.IsNullOrEmpty(i.SizeName) ? new Size { Name = i.SizeName } : null,
                        Color = !string.IsNullOrEmpty(i.ColorName) ? new Color { Name = i.ColorName } : null,
                        Inventory = new Inventory { Quantity = i.MaxStock, ReservedQuantity = 0 }
                    }
                }).ToList()
            };

            var subTotal = dto.Subtotal;
            var deliveryFee = subTotal >= 50.00m || subTotal == 0 ? 0.00m : 5.00m;

            return new CartViewModel
            {
                Cart = cart,
                SubTotal = subTotal,
                DeliveryFee = deliveryFee,
                Total = subTotal + deliveryFee
            };
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var response = await _apiClient.GetAsync<CartDto>("api/cart");
            var dto = response?.Data ?? new CartDto();
            var model = MapDtoToViewModel(dto);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetDrawerCart()
        {
            var response = await _apiClient.GetAsync<CartDto>("api/cart");
            return Json(response?.Data ?? new CartDto());
        }

        [HttpGet]
        public IActionResult SidebarPartial()
        {
            return PartialView("_CartSidebar");
        }

        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Json(new { count = 0 });
            }

            var response = await _apiClient.GetAsync<CartDto>("api/cart");
            var count = response?.Data?.TotalItems ?? 0;
            return Json(new { count });
        }

        [HttpPost]
        public async Task<IActionResult> Add(Guid variantId, int quantity = 1, string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                var loginUrl = Url.Action("Login", "Account", new { returnUrl = returnUrl ?? Request.Headers["Referer"].ToString() }) ?? "/Account/Login";
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, requireLogin = true, redirectUrl = loginUrl, message = "Please sign in to add items to your shopping bag." });
                }

                TempData["Info"] = "Please sign in to add items to your shopping bag.";
                return Redirect(loginUrl);
            }

            var request = new AddToCartRequestDto
            {
                VariantId = variantId,
                Quantity = quantity > 0 ? quantity : 1
            };

            var response = await _apiClient.PostAsync<AddToCartRequestDto, object>("api/cart/items", request);
            var success = response?.Success ?? false;
            var message = response?.Message ?? (success ? "Item added to bag." : "Failed to add item to bag.");

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
                var cartResponse = await _apiClient.GetAsync<CartDto>("api/cart");
                var cartCount = cartResponse?.Data?.TotalItems ?? 0;
                return Json(new { success, message, cartCount });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AddItem([FromBody] AddToCartRequestDto request)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Json(new { success = false, requireLogin = true, message = "Please sign in to add items to your shopping bag." });
            }

            var response = await _apiClient.PostAsync<AddToCartRequestDto, object>("api/cart/items", request);
            var cartResponse = await _apiClient.GetAsync<CartDto>("api/cart");
            var cartCount = cartResponse?.Data?.TotalItems ?? 0;
            return Json(new { success = response?.Success ?? false, message = response?.Message, cartCount });
        }

        [HttpPost]
        public async Task<IActionResult> Update(Guid id, int quantity, string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Not authenticated." });
                return RedirectToAction("Login", "Account");
            }

            var request = new UpdateCartItemRequestDto
            {
                CartItemId = id,
                Quantity = quantity
            };

            var response = await _apiClient.PutAsync<UpdateCartItemRequestDto, object>($"api/cart/items/{id}", request);
            var success = response?.Success ?? false;
            var message = response?.Message ?? (success ? "Bag updated." : "Failed to update bag.");

            if (success) TempData["Success"] = message;
            else TempData["Error"] = message;

            if (IsAjaxRequest())
            {
                var cartResponse = await _apiClient.GetAsync<CartDto>("api/cart");
                var cartCount = cartResponse?.Data?.TotalItems ?? 0;
                return Json(new { success, message, cartCount });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Remove(Guid id, string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                if (IsAjaxRequest()) return Json(new { success = false, message = "Not authenticated." });
                return RedirectToAction("Login", "Account");
            }

            var response = await _apiClient.DeleteAsync<object>($"api/cart/items/{id}");
            var success = response?.Success ?? false;
            var message = response?.Message ?? (success ? "Item removed from bag." : "Failed to remove item.");

            if (success) TempData["Success"] = message;
            else TempData["Error"] = message;

            if (IsAjaxRequest())
            {
                var cartResponse = await _apiClient.GetAsync<CartDto>("api/cart");
                var cartCount = cartResponse?.Data?.TotalItems ?? 0;
                return Json(new { success, message, cartCount });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Clear(string? returnUrl = null)
        {
            var cartResponse = await _apiClient.GetAsync<CartDto>("api/cart");
            if (cartResponse?.Data?.Items != null)
            {
                foreach (var item in cartResponse.Data.Items)
                {
                    await _apiClient.DeleteAsync<object>($"api/cart/items/{item.Id}");
                }
            }

            TempData["Success"] = "Shopping bag cleared.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetProductVariants(Guid productId)
        {
            var response = await _apiClient.GetAsync<ProductDetailDto>($"api/products/{productId}");
            if (response?.Data == null) return Json(new List<object>());

            var variants = response.Data.Variants.Select(v => new
            {
                id = v.Id,
                productId = v.ProductId,
                sizeId = v.SizeId,
                sizeName = v.SizeName ?? "",
                colorId = v.ColorId,
                colorName = v.ColorName ?? "",
                colorHex = v.ColorHex ?? "#000000",
                price = v.Price,
                compareAtPrice = v.CompareAtPrice,
                stock = v.StockQuantity,
                sku = v.Sku
            });

            return Json(variants);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeVariant(Guid id, Guid newVariantId, string? returnUrl = null)
        {
            var cartResponse = await _apiClient.GetAsync<CartDto>("api/cart");
            var item = cartResponse?.Data?.Items.FirstOrDefault(i => i.Id == id);
            var quantity = item?.Quantity ?? 1;

            await _apiClient.DeleteAsync<object>($"api/cart/items/{id}");
            var addResponse = await _apiClient.PostAsync<AddToCartRequestDto, object>("api/cart/items", new AddToCartRequestDto
            {
                VariantId = newVariantId,
                Quantity = quantity
            });

            var success = addResponse?.Success ?? false;
            var message = success ? "Item variant updated." : "Failed to update item variant.";

            if (IsAjaxRequest())
            {
                var updatedCart = await _apiClient.GetAsync<CartDto>("api/cart");
                return Json(new { success, message, cartCount = updatedCart?.Data?.TotalItems ?? 0 });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            return RedirectToAction("Index", "Checkout");
        }
    }
}
