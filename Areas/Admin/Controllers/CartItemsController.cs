
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CartItemsController : Controller
{
    private readonly AppDbContext _context;

    public CartItemsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: CARTITEMS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.CartItems.ToListAsync());
    }

    // GET: CARTITEMS/Details/5
    public async Task<IActionResult> Details(System.Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cartitem = await _context.CartItems
            .FirstOrDefaultAsync(m => m.Id == id);
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
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,CartId,VariantId,Quantity,Cart,Variant")] CartItem cartitem)
    {
        if (ModelState.IsValid)
        {
            _context.Add(cartitem);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(cartitem);
    }

    // GET: CARTITEMS/Edit/5
    public async Task<IActionResult> Edit(System.Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cartitem = await _context.CartItems.FindAsync(id);
        if (cartitem == null)
        {
            return NotFound();
        }
        return View(cartitem);
    }

    // POST: CARTITEMS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(System.Guid? id, [Bind("Id,CartId,VariantId,Quantity,Cart,Variant")] CartItem cartitem)
    {
        if (id != cartitem.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(cartitem);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CartItemExists(cartitem.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(cartitem);
    }

    // GET: CARTITEMS/Delete/5
    public async Task<IActionResult> Delete(System.Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cartitem = await _context.CartItems
            .FirstOrDefaultAsync(m => m.Id == id);
        if (cartitem == null)
        {
            return NotFound();
        }

        return View(cartitem);
    }

    // POST: CARTITEMS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(System.Guid? id)
    {
        var cartitem = await _context.CartItems.FindAsync(id);
        if (cartitem != null)
        {
            _context.CartItems.Remove(cartitem);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool CartItemExists(System.Guid? id)
    {
        return _context.CartItems.Any(e => e.Id == id);
    }
}
