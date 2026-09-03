
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class WishlistsController : Controller
{
    private readonly AppDbContext _context;

    public WishlistsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: WISHLISTS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Wishlists.ToListAsync());
    }

    // GET: WISHLISTS/Details/5
    public async Task<IActionResult> Details(System.Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var wishlist = await _context.Wishlists
            .FirstOrDefaultAsync(m => m.Id == id);
        if (wishlist == null)
        {
            return NotFound();
        }

        return View(wishlist);
    }

    // GET: WISHLISTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: WISHLISTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,CustomerId,Customer,Items")] Wishlist wishlist)
    {
        if (ModelState.IsValid)
        {
            _context.Add(wishlist);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(wishlist);
    }

    // GET: WISHLISTS/Edit/5
    public async Task<IActionResult> Edit(System.Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var wishlist = await _context.Wishlists.FindAsync(id);
        if (wishlist == null)
        {
            return NotFound();
        }
        return View(wishlist);
    }

    // POST: WISHLISTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(System.Guid? id, [Bind("Id,CustomerId,Customer,Items")] Wishlist wishlist)
    {
        if (id != wishlist.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(wishlist);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WishlistExists(wishlist.Id))
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
        return View(wishlist);
    }

    // GET: WISHLISTS/Delete/5
    public async Task<IActionResult> Delete(System.Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var wishlist = await _context.Wishlists
            .FirstOrDefaultAsync(m => m.Id == id);
        if (wishlist == null)
        {
            return NotFound();
        }

        return View(wishlist);
    }

    // POST: WISHLISTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(System.Guid? id)
    {
        var wishlist = await _context.Wishlists.FindAsync(id);
        if (wishlist != null)
        {
            _context.Wishlists.Remove(wishlist);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool WishlistExists(System.Guid? id)
    {
        return _context.Wishlists.Any(e => e.Id == id);
    }
}
