using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CartController(AppDbContext context) : Controller
{
    // GET: Admin/Cart?customerId=xxx
    public async Task<IActionResult> Index(Guid customerId)
    {
        ViewBag.CustomerId = customerId;

        var cart = await context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v.Product)
            .Include(c => c.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v.Color)
            .Include(c => c.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v.Size)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        return View(cart);
    }

    // POST: Admin/Cart/AddToCart
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(Guid customerId, Guid variantId, int quantity = 1)
    {
        if (quantity <= 0) quantity = 1;

        var cart = await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (cart == null)
        {
            cart = new Cart
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                CreatedAt = DateTime.UtcNow,
                Items = new List<CartItem>()
            };
            context.Carts.Add(cart);
        }

        var cartItem = cart.Items.FirstOrDefault(i => i.VariantId == variantId);
        if (cartItem != null)
        {
            cartItem.Quantity += quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                VariantId = variantId,
                Quantity = quantity
            });
        }

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { customerId });
    }

    // POST: Admin/Cart/UpdateQuantity
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(Guid customerId, Guid cartItemId, int quantity)
    {
        var cartItem = await context.CartItems.FindAsync(cartItemId);

        if (cartItem != null)
        {
            if (quantity > 0)
            {
                cartItem.Quantity = quantity;
            }
            else
            {
                context.CartItems.Remove(cartItem);
            }
            await context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index), new { customerId });
    }

    // POST: Admin/Cart/RemoveItem
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(Guid customerId, Guid cartItemId)
    {
        var cartItem = await context.CartItems.FindAsync(cartItemId);
        if (cartItem != null)
        {
            context.CartItems.Remove(cartItem);
            await context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index), new { customerId });
    }

    // POST: Admin/Cart/ClearCart
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearCart(Guid customerId)
    {
        var cart = await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (cart != null && cart.Items.Any())
        {
            context.CartItems.RemoveRange(cart.Items);
            await context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index), new { customerId });
    }
}