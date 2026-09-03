
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class WishlistItemsController : Controller
{
    private readonly AppDbContext _context;

    public WishlistItemsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: WISHLISTITEMS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.WishlistItems.ToListAsync());
    }

    // GET: WISHLISTITEMS/Details/5
    public async Task<IActionResult> Details(System.Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var wishlistitem = await _context.WishlistItems
            .FirstOrDefaultAsync(m => m.Id == id);
        if (wishlistitem == null)
        {
            return NotFound();
        }

        return View(wishlistitem);
    }

    // GET: WISHLISTITEMS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: WISHLISTITEMS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,WishlistId,VariantId,Wishlist,Variant")] WishlistItem wishlistitem)
    {
        if (ModelState.IsValid)
        {
            _context.Add(wishlistitem);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(wishlistitem);
    }

    // GET: WISHLISTITEMS/Edit/5
    public async Task<IActionResult> Edit(System.Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var wishlistitem = await _context.WishlistItems.FindAsync(id);
        if (wishlistitem == null)
        {
            return NotFound();
        }
        return View(wishlistitem);
    }

    // POST: WISHLISTITEMS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(System.Guid? id, [Bind("Id,WishlistId,VariantId,Wishlist,Variant")] WishlistItem wishlistitem)
    {
        if (id != wishlistitem.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(wishlistitem);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WishlistItemExists(wishlistitem.Id))
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
        return View(wishlistitem);
    }

    // GET: WISHLISTITEMS/Delete/5
    public async Task<IActionResult> Delete(System.Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var wishlistitem = await _context.WishlistItems
            .FirstOrDefaultAsync(m => m.Id == id);
        if (wishlistitem == null)
        {
            return NotFound();
        }

        return View(wishlistitem);
    }

    // POST: WISHLISTITEMS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(System.Guid? id)
    {
        var wishlistitem = await _context.WishlistItems.FindAsync(id);
        if (wishlistitem != null)
        {
            _context.WishlistItems.Remove(wishlistitem);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool WishlistItemExists(System.Guid? id)
    {
        return _context.WishlistItems.Any(e => e.Id == id);
    }
}
