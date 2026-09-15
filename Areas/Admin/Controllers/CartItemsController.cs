
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class CartItemsController : Controller
{
    private readonly ICartService _cartService;

    public CartItemsController(ICartService cartService)
    {
        _cartService = cartService;
    }

    // GET: CARTITEMS
    public async Task<IActionResult> Index()    
    {
        return View(await _cartService.GetAllCartItemsAsync());
    }

    // GET: CARTITEMS/Details/5
    public async Task<IActionResult> Details(Guid? id)
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

        return View(cartitem);
    }

    // GET: CARTITEMS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: CARTITEMS/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,CartId,VariantId,Quantity,Cart,Variant")] CartItem cartitem)
    {
        if (ModelState.IsValid)
        {
            await _cartService.CreateCartItemAsync(cartitem);
            return RedirectToAction(nameof(Index));
        }
        return View(cartitem);
    }

    // GET: CARTITEMS/Edit/5
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
        return View(cartitem);
    }

    // POST: CARTITEMS/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,CartId,VariantId,Quantity,Cart,Variant")] CartItem cartitem)
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
        return View(cartitem);
    }

    // GET: CARTITEMS/Delete/5
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

        return View(cartitem);
    }

    // POST: CARTITEMS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _cartService.DeleteCartItemAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
