using System.Text.Json;
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
        private const string GuestCartSessionKey = "guest_cart_data";

        public CartController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool IsAjaxRequest() =>
            Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
            Request.Headers.Accept.ToString().Contains("application/json");

        private CartDto GetGuestCart()
        {
            var json = HttpContext.Session.GetString(GuestCartSessionKey);
            if (string.IsNullOrEmpty(json))
            {
                return new CartDto { Id = Guid.NewGuid(), ShippingFee = 5.00m };
            }
            try
            {
                return JsonSerializer.Deserialize<CartDto>(json) ?? new CartDto { Id = Guid.NewGuid(), ShippingFee = 5.00m };
            }
            catch
            {
                return new CartDto { Id = Guid.NewGuid(), ShippingFee = 5.00m };
            }
        }

        private void SaveGuestCart(CartDto cart)
        {
            var json = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString(GuestCartSessionKey, json);
        }

        private void ClearGuestCart()
        {
            HttpContext.Session.Remove(GuestCartSessionKey);
        }

        private async Task SyncGuestCartIfAuthenticatedAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var guestCart = GetGuestCart();
                if (guestCart.Items.Any())
                {
                    foreach (var item in guestCart.Items)
                    {
                        if (item.VariantId.HasValue)
                        {
                            await _apiClient.PostAsync<AddToCartRequestDto, object>("api/cart/items", new AddToCartRequestDto
                            {
                                ProductId = item.ProductId,
                                VariantId = item.VariantId.Value,
                                Quantity = item.Quantity
                            });
                        }
                    }
                    ClearGuestCart();
                }
            }
        }

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

        private async Task<CartDto> GetEffectiveCartDtoAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                await SyncGuestCartIfAuthenticatedAsync();
                var response = await _apiClient.GetAsync<CartDto>("api/cart");
                return response?.Data ?? new CartDto();
            }
            return GetGuestCart();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dto = await GetEffectiveCartDtoAsync();
            var model = MapDtoToViewModel(dto);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetDrawerCart()
        {
            var dto = await GetEffectiveCartDtoAsync();
            return Json(dto);
        }

        [HttpGet]
        public async Task<IActionResult> SidebarPartial()
        {
            var dto = await GetEffectiveCartDtoAsync();
            var model = MapDtoToViewModel(dto);
            return PartialView("_CartSidebar", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            var dto = await GetEffectiveCartDtoAsync();
            return Json(new { count = dto.TotalItems });
        }

        [HttpPost]
        public async Task<IActionResult> Add(Guid? variantId, Guid? productId, int quantity = 1, string? returnUrl = null)
        {
            var qty = quantity > 0 ? quantity : 1;
            var targetVariantId = variantId.GetValueOrDefault();
            var targetProductId = productId.GetValueOrDefault();

            // If variantId is not specified or empty, but productId is available, find default variant
            if (targetVariantId == Guid.Empty && targetProductId != Guid.Empty)
            {
                var defVariantRes = await _apiClient.GetAsync<CartItemDto>($"api/products/{targetProductId}/default-variant");
                if (defVariantRes?.Success == true && defVariantRes.Data != null)
                {
                    targetVariantId = defVariantRes.Data.VariantId.GetValueOrDefault();
                }
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                var request = new AddToCartRequestDto
                {
                    ProductId = targetProductId != Guid.Empty ? targetProductId : null,
                    VariantId = targetVariantId != Guid.Empty ? targetVariantId : null,
                    Quantity = qty
                };

                var response = await _apiClient.PostAsync<AddToCartRequestDto, object>("api/cart/items", request);
                var success = response?.Success ?? false;
                var message = response?.Message ?? (success ? "Item added to bag." : "Failed to add item to bag.");

                if (success) TempData["Success"] = message;
                else TempData["Error"] = message;

                if (IsAjaxRequest())
                {
                    var cart = await GetEffectiveCartDtoAsync();
                    return Json(new { success, message, cartCount = cart.TotalItems });
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                // Guest cart handling
                CartItemDto? item = null;

                if (targetVariantId != Guid.Empty)
                {
                    var variantRes = await _apiClient.GetAsync<CartItemDto>($"api/products/variant/{targetVariantId}");
                    item = variantRes?.Data;
                }

                if (item == null && targetProductId != Guid.Empty)
                {
                    var defVariantRes = await _apiClient.GetAsync<CartItemDto>($"api/products/{targetProductId}/default-variant");
                    item = defVariantRes?.Data;
                }

                if (item == null)
                {
                    if (IsAjaxRequest()) return Json(new { success = false, message = "Product variant not found." });
                    TempData["Error"] = "Product variant not found.";
                    return RedirectToAction(nameof(Index));
                }

                var guestCart = GetGuestCart();
                var existing = guestCart.Items.FirstOrDefault(i => i.VariantId == item.VariantId);
                if (existing != null)
                {
                    existing.Quantity += qty;
                }
                else
                {
                    item.Quantity = qty;
                    guestCart.Items.Add(item);
                }

                SaveGuestCart(guestCart);

                if (IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Item added to your shopping bag.", cartCount = guestCart.TotalItems });
                }

                TempData["Success"] = "Item added to your shopping bag.";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddItem([FromBody] AddToCartRequestDto request)
        {
            return await Add(request.VariantId, request.ProductId, request.Quantity);
        }

        [HttpPost]
        public async Task<IActionResult> Update(Guid id, int quantity, string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
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
                    var cart = await GetEffectiveCartDtoAsync();
                    return Json(new { success, message, cartCount = cart.TotalItems });
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var guestCart = GetGuestCart();
                var item = guestCart.Items.FirstOrDefault(i => i.Id == id || i.VariantId == id);
                if (item != null)
                {
                    if (quantity <= 0) guestCart.Items.Remove(item);
                    else item.Quantity = quantity;
                    SaveGuestCart(guestCart);
                }

                if (IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Bag updated.", cartCount = guestCart.TotalItems });
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Remove(Guid id, string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var response = await _apiClient.DeleteAsync<object>($"api/cart/items/{id}");
                var success = response?.Success ?? false;
                var message = response?.Message ?? (success ? "Item removed from bag." : "Failed to remove item.");

                if (success) TempData["Success"] = message;
                else TempData["Error"] = message;

                if (IsAjaxRequest())
                {
                    var cart = await GetEffectiveCartDtoAsync();
                    return Json(new { success, message, cartCount = cart.TotalItems });
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var guestCart = GetGuestCart();
                var item = guestCart.Items.FirstOrDefault(i => i.Id == id || i.VariantId == id);
                if (item != null)
                {
                    guestCart.Items.Remove(item);
                    SaveGuestCart(guestCart);
                }

                if (IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Item removed from bag.", cartCount = guestCart.TotalItems });
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Clear(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var cartResponse = await _apiClient.GetAsync<CartDto>("api/cart");
                if (cartResponse?.Data?.Items != null)
                {
                    foreach (var item in cartResponse.Data.Items)
                    {
                        await _apiClient.DeleteAsync<object>($"api/cart/items/{item.Id}");
                    }
                }
            }
            else
            {
                ClearGuestCart();
            }

            TempData["Success"] = "Shopping bag cleared.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
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
            await Remove(id);
            await Add(variantId: newVariantId, productId: null, quantity: 1);

            if (IsAjaxRequest())
            {
                var cart = await GetEffectiveCartDtoAsync();
                return Json(new { success = true, message = "Variant updated.", cartCount = cart.TotalItems });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            return RedirectToAction("Index", "Checkout");
        }
    }
}
