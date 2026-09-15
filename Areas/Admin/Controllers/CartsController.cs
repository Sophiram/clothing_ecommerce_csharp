using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class CartController : Controller
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    // GET: Admin/Cart?customerId=xxx
    public async Task<IActionResult> Index(Guid customerId)
    {
        ViewBag.CustomerId = customerId;

        var cart = await _cartService.GetAdminCustomerCartAsync(customerId);

        return View(cart);
    }

    // POST: Admin/Cart/AddToCart
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(Guid customerId, Guid variantId, int quantity = 1)
    {
        if (quantity <= 0) quantity = 1;

        await _cartService.AddItemAsync(customerId, variantId, quantity);

        return RedirectToAction(nameof(Index), new { customerId });
    }

    // POST: Admin/Cart/UpdateQuantity
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(Guid customerId, Guid cartItemId, int quantity)
    {
        await _cartService.UpdateQuantityAsync(customerId, cartItemId, quantity);

        return RedirectToAction(nameof(Index), new { customerId });
    }

    // POST: Admin/Cart/RemoveItem
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(Guid customerId, Guid cartItemId)
    {
        await _cartService.RemoveItemAsync(customerId, cartItemId);

        return RedirectToAction(nameof(Index), new { customerId });
    }

    // POST: Admin/Cart/ClearCart
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearCart(Guid customerId)
    {
        await _cartService.ClearCartAsync(customerId);

        return RedirectToAction(nameof(Index), new { customerId });
    }
}